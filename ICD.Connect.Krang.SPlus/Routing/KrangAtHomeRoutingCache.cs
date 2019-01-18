using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Routing
{
	public sealed class KrangAtHomeRoutingCache : IDisposable, IConsoleNode
	{
		/// <summary>
		/// Should contain all sources
		/// </summary>
		private readonly Dictionary<eConnectionType, IcdHashSet<ISource>> m_Sources;
		private readonly SafeCriticalSection m_SourcesSection;

		/// <summary>
		/// Added/Removed as used
		/// </summary>
		private readonly Dictionary<ISource, IcdHashSet<IRoom>> m_SourceInUseCache;
		private readonly SafeCriticalSection m_SourceInUseCacheSection;

		/// <summary>
		/// Added/Removed as used
		/// </summary>
		private readonly Dictionary<IDestination, IcdHashSet<IRoom>> m_DestinationToRoomsCache;
		private readonly SafeCriticalSection m_DestinationToRoomCacheSection;


		public KrangAtHomeRoutingCache()
		{
			m_Sources = new Dictionary<eConnectionType, IcdHashSet<ISource>>();
			m_SourcesSection = new SafeCriticalSection();

			m_SourceInUseCache = new Dictionary<ISource, IcdHashSet<IRoom>>();
			m_SourceInUseCacheSection = new SafeCriticalSection();

			m_DestinationToRoomsCache = new Dictionary<IDestination, IcdHashSet<IRoom>>();
			m_DestinationToRoomCacheSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Get all sources of the specified type from the cache
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<ISource> GetSouresForType(eConnectionType type)
		{
			if (EnumUtils.HasMultipleFlags(type))
				throw new ArgumentException("type can onyl be a single flag");

			m_SourcesSection.Enter();
			try
			{
				IcdHashSet<ISource> sources;
				if (m_Sources.TryGetValue(type, out sources))
					return sources;
				return Enumerable.Empty<ISource>();
			}
			finally
			{
				m_SourcesSection.Leave();
			}
		}

		/// <summary>
		/// Gets all the rooms associated with the given destination
		/// </summary>
		/// <param name="destination"></param>
		/// <returns></returns>
		public IEnumerable<IRoom> GetRoomsForDestination(IDestination destination)
		{
			m_DestinationToRoomCacheSection.Enter();

			try
			{
				IcdHashSet<IRoom> roomsForDestination;
				if (!m_DestinationToRoomsCache.TryGetValue(destination, out roomsForDestination))
				{
					//todo: Caculate rooms for unknown destination
					throw new NotImplementedException();
				}
				return roomsForDestination;

			}
			finally
			{
				m_DestinationToRoomCacheSection.Leave();
			}
		}

		/// <summary>
		/// Gets all the rooms associated with the given destinations
		/// </summary>
		/// <param name="destinations"></param>
		/// <returns></returns>
		public IEnumerable<IRoom> GetRoomsForDestinations(IEnumerable<IDestination> destinations)
		{
			IcdHashSet<IRoom> roomsForDestinations = new IcdHashSet<IRoom>();
			foreach(IDestination destination in destinations)
				roomsForDestinations.AddRange(GetRoomsForDestination(destination));
			return roomsForDestinations;
		}

		/// <summary>
		/// Gets all the rooms the source is used in from the cache
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public IEnumerable<IRoom> GetRoomsForSource(ISource source)
		{
			m_SourceInUseCacheSection.Enter();

			try
			{
				IcdHashSet<IRoom> rooms;
				if (!m_SourceInUseCache.TryGetValue(source, out rooms))
					return Enumerable.Empty<IRoom>();
				return rooms.ToList(rooms.Count);
			}
			finally
			{
				m_SourceInUseCacheSection.Leave();
			}
		}

		/// <summary>
		/// Removes the give source from the InUse cache for all rooms
		/// </summary>
		/// <param name="source">Source to remove</param>
		/// <param name="removedRooms">Rooms source was previously used by</param>
		/// <returns>True if the InUse cache changed, false if not</returns>
		internal bool SourceClearCachedRooms(ISource source, out IcdHashSet<IRoom> removedRooms)
		{
			removedRooms = null;
			if (source == null)
				return false;

			m_SourceInUseCacheSection.Enter();
			try
			{
				IcdHashSet<IRoom> rooms;
				if (!m_SourceInUseCache.TryGetValue(source, out rooms))
					return false;
				if (rooms.Count == 0)
					return false;
				removedRooms = rooms.ToIcdHashSet();
				m_SourceInUseCache.Remove(source);
				return true;

			}
			finally
			{
				m_SourceInUseCacheSection.Leave();
			}
		}

		internal bool SourceSetCachedRooms(ISource source, IEnumerable<IRoom> roomsForSource)
		{
			bool unusedBool;
			IEnumerable<IRoom> unusedRooms;
			return SourceSetCachedRooms(source, roomsForSource, out unusedRooms, out unusedBool);
		}

		/// <summary>
		/// Sets the cached rooms for the source to be the passed in collection
		/// Removes and rooms not in the collection
		/// </summary>
		/// <param name="source">source</param>
		/// <param name="roomsForSource">rooms source is used in</param>
		/// <param name="unusedRooms">rooms that no longer have the source routed</param>
		/// <param name="previouslyUnusedSource">true if the source wasn't used before</param>
		/// <returns>true if the cache changed</returns>
		internal bool SourceSetCachedRooms(ISource source, IEnumerable<IRoom> roomsForSource, out IEnumerable<IRoom> unusedRooms, out bool previouslyUnusedSource)
		{
			IcdHashSet<IRoom> roomsForSourceLocal = new IcdHashSet<IRoom>(roomsForSource);
			
			previouslyUnusedSource = false;
			m_SourceInUseCacheSection.Enter();
			try
			{
				// Get Current Room Collection
				IcdHashSet<IRoom> roomsCollection;
				if (!m_SourceInUseCache.TryGetValue(source, out roomsCollection))
				{
					previouslyUnusedSource = true;
					m_SourceInUseCache.Add(source, roomsCollection = new IcdHashSet<IRoom>());
				}

				//Get Now Unused rooms
				IcdHashSet<IRoom> unusedRoomsLocal = new IcdHashSet<IRoom>();
				if (!previouslyUnusedSource) 
					unusedRoomsLocal.AddRange(roomsCollection.Except(roomsForSourceLocal));

				unusedRoomsLocal.ForEach(r=> roomsCollection.Remove(r));

				unusedRooms = unusedRoomsLocal.ToArray(unusedRoomsLocal.Count);

				bool changed = unusedRoomsLocal.Count > 0;
				foreach(var room in roomsForSourceLocal)
					changed |= roomsCollection.Add(room);
				return changed;

			}
			finally
			{
				m_SourceInUseCacheSection.Leave();
			}
		}

		public void Dispose()
		{
			//todo: Dispose all the things?
		}


		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return "KrangAtHomeRoutingCache"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Routing Cache for KrangAtHome Specific items"; } }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			// todo: Add console commands to print caches
			yield break;
		}

		#endregion
	}
}