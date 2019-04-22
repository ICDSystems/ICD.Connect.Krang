using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Routing.RoutingCaches;
using ICD.Connect.Routing.RoutingGraphs;

namespace ICD.Connect.Krang.SPlus.Routing
{
	public sealed class KrangAtHomeRouting : IDisposable, IConsoleNode
	{
		private bool m_Debug;

		private readonly KrangAtHomeRoutingCache m_KrangAtHomeCache;
		private RoutingCache m_RoutingCache;

		public event EventHandler<SourceInUseUpdatedEventArgs> OnSourceInUseUpdated;
		public event EventHandler<SourceRoomsUsedUpdatedEventArgs> OnSourceRoomsUsedUpdated;

		public KrangAtHomeRoutingCache KrangAtHomeRoutingCache { get { return m_KrangAtHomeCache; } }

		public KrangAtHomeRouting(IRoutingGraph routingGraph)
		{
			m_KrangAtHomeCache = new KrangAtHomeRoutingCache();

			m_RoutingCache = routingGraph.RoutingCache;
			Subscribe(m_RoutingCache);

			
		}

		#region RoutingCache Callbacks

		private void Subscribe(RoutingCache routingCache)
		{
			if (routingCache == null)
				return;

			routingCache.OnSourceDestinationRouteChanged += RoutingCacheOnSourceDestinationRouteChanged;
		}

		private void Unsubscribe(RoutingCache routingCache)
		{
			if (routingCache == null)
				return;

			routingCache.OnSourceDestinationRouteChanged -= RoutingCacheOnSourceDestinationRouteChanged;
		}

		private void RoutingCacheOnSourceDestinationRouteChanged(object sender, SourceDestinationRouteChangedEventArgs args)
		{
			UpdateSourceDestinationsCache(args.Type);
		}

		private void UpdateSourceDestinationsCache(eConnectionType types)
		{
			// Todo: Debug
			if (m_Debug)
				IcdConsole.PrintLine(eConsoleColor.Magenta, "K@HR: UpdateSourceDestinationsCache");

			Dictionary<ISource, eConnectionType> sourcesToUpdate = GetSourcesForTypes(types);

			Dictionary<ISource, IcdHashSet<IDestination>> destinationsForSources =
				new Dictionary<ISource, IcdHashSet<IDestination>>();
			IcdHashSet<ISource> sourcesWithoutDestination = new IcdHashSet<ISource>();

			foreach (var kvp in sourcesToUpdate)
			{
				// Todo: Debug
				if (m_Debug)
					IcdConsole.PrintLine(eConsoleColor.Magenta, "K@HR: Updating Destinations for {0}", kvp.Key);

				IcdHashSet<IDestination> destinationsForSource = new IcdHashSet<IDestination>();
				foreach (var type in EnumUtils.GetFlagsExceptNone(kvp.Value))
				{
					destinationsForSource.AddRange(m_RoutingCache.GetDestinationsForSource(kvp.Key, type));
				}
				if (destinationsForSource.Count > 0)
				{
					// Todo: Debug
					if (m_Debug)
						IcdConsole.PrintLine(eConsoleColor.Magenta, "K@HR: Source {0} has {1} destinations", kvp.Key, destinationsForSource.Count);
					destinationsForSources[kvp.Key] = destinationsForSource;
				}
				else
				{
					// Todo: Debug
					if (m_Debug)
						IcdConsole.PrintLine(eConsoleColor.Magenta, "K@HR: Source {0} has no destinations", kvp.Key);

					sourcesWithoutDestination.Add(kvp.Key);
				}
			}

			IcdHashSet<ISource> changedSources = new IcdHashSet<ISource>();
			IcdHashSet<ISource> newInUseSources = new IcdHashSet<ISource>();

			foreach (var source in sourcesWithoutDestination)
			{
				IcdHashSet<IRoom> removedRooms;
				if (m_KrangAtHomeCache.SourceClearCachedRooms(source, out removedRooms))
					if (removedRooms.Count > 0)
					{
						// Todo: Debug
						if (m_Debug)
							IcdConsole.PrintLine(eConsoleColor.Magenta, "K@HR: Unused source {0} was removed from {1} rooms", source,
							                     removedRooms.Count);
						changedSources.Add(source);
					}
					else
					{
						// Todo: Debug
						if (m_Debug)
							IcdConsole.PrintLine(eConsoleColor.Magenta, "K@HR: Unused source {0} was not routed to any rooms", source);
					}
			}

			foreach (var kvp in destinationsForSources)
			{
				List<IRoom> roomsForSource = m_KrangAtHomeCache.GetRoomsForDestinations(kvp.Value).ToList();
				IEnumerable<IRoom> unusedRooms;
				bool previoulsyUnusedSource;
				
				// Todo: Debug
				if (m_Debug)
					IcdConsole.PrintLine(eConsoleColor.Magenta, "K@HR: Change source {0} used in {1} rooms", kvp.Key, roomsForSource.Count);

				if (m_KrangAtHomeCache.SourceSetCachedRooms(kvp.Key, roomsForSource, out unusedRooms, out previoulsyUnusedSource))
				{
					// Todo: Debug
					if (m_Debug)
						IcdConsole.PrintLine(eConsoleColor.Magenta, "K@HR: Changed source {0} changed routed rooms", kvp.Key);
					changedSources.Add(kvp.Key);
				}
				if (previoulsyUnusedSource)
				{
					// Todo: Debug
					if (m_Debug)
						IcdConsole.PrintLine(eConsoleColor.Magenta, "K@HR: Changed source {0} previoulsy unused", kvp.Key);
					newInUseSources.Add(kvp.Key);
				}
			}


			foreach (var source in sourcesWithoutDestination)
				OnSourceInUseUpdated.Raise(this, new SourceInUseUpdatedEventArgs(source, false));

			foreach (var source in newInUseSources)
				OnSourceInUseUpdated.Raise(this, new SourceInUseUpdatedEventArgs(source, true));

			foreach (var source in changedSources)
				OnSourceRoomsUsedUpdated.Raise(this,
				                               new SourceRoomsUsedUpdatedEventArgs(source,
				                                                                   m_KrangAtHomeCache.GetRoomsForSource(source)));
		}

		private Dictionary<ISource, eConnectionType> GetSourcesForTypes(eConnectionType types)
		{
			Dictionary<ISource, eConnectionType> sourcesForType = new Dictionary<ISource, eConnectionType>();

			foreach (eConnectionType type in EnumUtils.GetFlagsExceptNone(types))
			{
				foreach (ISource source in m_KrangAtHomeCache.GetSouresForType(type))
				{
					eConnectionType typeForSource;
					if (sourcesForType.TryGetValue(source, out typeForSource))
						typeForSource |= type;
					else
						typeForSource = type;

					sourcesForType[source] = typeForSource;
				}
			}

			return sourcesForType;
		}

		/// <summary>
		/// Adds the source to the cache
		/// </summary>
		public void AddSources(IEnumerable<ISource> sources)
		{
			if (m_KrangAtHomeCache != null)
				m_KrangAtHomeCache.AddSources(sources);

		}

		

		#endregion

		public void Dispose()
		{
			Unsubscribe(m_RoutingCache);
			m_RoutingCache = null;
		}

		private void SetDebug(bool debug)
		{
			m_Debug = debug;
		}

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return "KrangAtHomeRouting"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Routing operations for KrangAtHome"; } }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return m_KrangAtHomeCache;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Debug", m_Debug);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new GenericConsoleCommand<bool>("SetDebug", "SetDebug <true|false>", d => SetDebug(d));
		}

		#endregion
	}
}