using System;
using System.Collections.Generic;
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
	public class KrangAtHomeRouting : IDisposable, IConsoleNode
	{
		private readonly KrangAtHomeRoutingCache m_KrangAtHomeCache;
		private RoutingCache m_RoutingCache;

		public event EventHandler<SourceInUseUpdatedEventArgs> OnSourceInUseUpdated;
		public event EventHandler<SourceRoomsUsedUpdatedEventArgs> OnSourceRoomsUsedUpdated;

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
			Dictionary<ISource, eConnectionType> sourcesToUpdate = GetSourcesForTypes(types);

			Dictionary<ISource, IcdHashSet<IDestination>> destinationsForSources =
				new Dictionary<ISource, IcdHashSet<IDestination>>();
			IcdHashSet<ISource> sourcesWithoutDestination = new IcdHashSet<ISource>();

			foreach (var kvp in sourcesToUpdate)
			{
				IcdHashSet<IDestination> destinationsForSource = new IcdHashSet<IDestination>();
				foreach (var type in EnumUtils.GetFlagsExceptNone(kvp.Value))
				{
					destinationsForSource.AddRange(m_RoutingCache.GetDestinationsForSource(kvp.Key, type));
				}
				if (destinationsForSource.Count > 0)
					destinationsForSources[kvp.Key] = destinationsForSource;
				else
					sourcesWithoutDestination.Add(kvp.Key);
			}

			IcdHashSet<ISource> changedSources = new IcdHashSet<ISource>();
			IcdHashSet<ISource> newInUseSources = new IcdHashSet<ISource>();

			foreach (var source in sourcesWithoutDestination)
			{
				IcdHashSet<IRoom> removedRooms;
				m_KrangAtHomeCache.SourceClearCachedRooms(source, out removedRooms);
			}

			foreach (var kvp in destinationsForSources)
			{
				IEnumerable<IRoom> roomsForSource = m_KrangAtHomeCache.GetRoomsForDestinations(kvp.Value);
				IEnumerable<IRoom> unusedRooms;
				bool previoulsyUnusedSource;
				if (m_KrangAtHomeCache.SourceSetCachedRooms(kvp.Key, roomsForSource, out unusedRooms, out previoulsyUnusedSource))
					changedSources.Add(kvp.Key);
				if (previoulsyUnusedSource)
					newInUseSources.Add(kvp.Key);
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

		

		#endregion

		public void Dispose()
		{
			Unsubscribe(m_RoutingCache);
			m_RoutingCache = null;
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
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield break;
		}

		#endregion
	}
}