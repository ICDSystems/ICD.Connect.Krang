using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.Audio.Controls;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Endpoints;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Routing.PathFinding;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Settings.Core;
using ICD.Connect.Settings.Simpl;

namespace ICD.Connect.Krang.SPlus.Rooms
{

	public enum eSourceTypeRouted
	{
		Video,
		Audio
	}

	public enum eCrosspointType
	{
		None,
		Lighting,
		Hvac
	}

	public sealed class SimplRoom : AbstractRoom<SimplRoomSettings>, IKrangAtHomeRoom, ISimplOriginator
	{
		

		#region Events

		public event EventHandler OnActiveSourcesChange;

		public event EventHandler<GenericEventArgs<IVolumeDeviceControl>> OnVolumeControlChanged; 

		#endregion

		#region Fields

		private readonly Dictionary<ushort, eCrosspointType> m_Crosspoints;
		private readonly SafeCriticalSection m_CrosspointsSection;

		private readonly IcdHashSet<ISource> m_CachedActiveSources;

		private IRoutingGraph m_SubscribedRoutingGraph;

		private IPathFinder m_PathFinder;

		private IVolumeDeviceControl m_VolumeControl;

		#endregion

		#region Properties

		private IPathFinder PathFinder
		{
			get { return m_PathFinder = m_PathFinder ?? new DefaultPathFinder(m_SubscribedRoutingGraph, Id); }
		}

		public IVolumeDeviceControl VolumeControl
		{
			get { return m_VolumeControl; }
			private set
			{
				if (value == m_VolumeControl)
					return;

				m_VolumeControl = value;

				OnVolumeControlChanged.Raise(this, new GenericEventArgs<IVolumeDeviceControl>(m_VolumeControl));

			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public SimplRoom()
		{
			m_Crosspoints = new Dictionary<ushort, eCrosspointType>();
			m_CrosspointsSection = new SafeCriticalSection();

			m_CachedActiveSources = new IcdHashSet<ISource>();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnActiveSourcesChange = null;

			m_CachedActiveSources.Clear();

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Gets the crosspoints.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<ushort, eCrosspointType>> GetCrosspoints()
		{
			return m_CrosspointsSection.Execute(() => m_Crosspoints.OrderByKey().ToArray());
		}

		/// <summary>
		/// Sets the crosspoints.
		/// </summary>
		/// <param name="crosspoints"></param>
		public void SetCrosspoints(IEnumerable<KeyValuePair<ushort, eCrosspointType>> crosspoints)
		{
			m_CrosspointsSection.Enter();

			try
			{
				m_Crosspoints.Clear();
				m_Crosspoints.AddRange(crosspoints);
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the current, actively routed source.
		/// </summary>
		[CanBeNull]
		public ISimplSource GetSource()
		{
			ISource source = m_CachedActiveSources.OrderBy(s => s.Id).FirstOrDefault();

			ISimplSource simplSource = source as ISimplSource;

			return simplSource;
		}

		public void SetSource(ISimplSource source, eSourceTypeRouted type)
		{
			Route(source, type);
		}

		public void SetSourceId(int sourceId, eSourceTypeRouted type)
		{
			ISimplSource source = GetSourceId(sourceId);

			SetSource(source, type);

		}

		#endregion

		#region Routing

		/// <summary>
		/// Routes the source to all destinations in the current room.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="sourceType"></param>
		private void Route(ISimplSource source, eSourceTypeRouted sourceType)
		{
			// todo: implement sourceType handling

			if (source == null)
			{
				Unroute();
				return;
			}

			if (m_SubscribedRoutingGraph == null)
				return;

			Log(eSeverity.Informational, "Routing {0}", source);

			IEnumerable<IDestination> roomDestinations = GetRoomDestinations();

			IEnumerable<ConnectionPath> paths =
				PathBuilder.FindPaths()
				           .From(source)
				           .To(roomDestinations)
				           .OfType(source.ConnectionType)
				           .With(PathFinder);

			m_SubscribedRoutingGraph.RoutePaths(paths, Id);
		}

		/// <summary>
		/// Unroutes all the destinations in the room.
		/// </summary>
		private void Unroute()
		{
			Log(eSeverity.Informational, "Unrouting all");

			GetRoomDestinations().ForEach(Unroute);
		}

		/// <summary>
		/// Unroutes all sources from the destination.
		/// </summary>
		/// <param name="destination"></param>
		private void Unroute(IDestination destination)
		{
			Log(eSeverity.Informational, "Unrouting {0}", destination);

			GetActiveSources(destination).ForEach(s => Unroute(s, destination));
		}

		/// <summary>
		/// Unroutes the source from the destination.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		private void Unroute(ISource source, IDestination destination)
		{
			if (m_SubscribedRoutingGraph == null)
				return;

			Log(eSeverity.Informational, "Unrouting {0} from {1}", source, destination);

			eConnectionType connectionType = EnumUtils.GetFlagsIntersection(source.ConnectionType, destination.ConnectionType);
			m_SubscribedRoutingGraph.Unroute(source, destination, connectionType, Id);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the source with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[CanBeNull]
		public ISimplSource GetSourceId(int id)
		{
			if (m_SubscribedRoutingGraph == null)
				return null;

			ISource source;
			m_SubscribedRoutingGraph.Sources.TryGetChild(id, out source);
			return source as ISimplSource;
		}

		/// <summary>
		/// Gets the sources currently routed to the room destinations.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<ISource> GetActiveRoomSources()
		{
			return GetRoomDestinations().SelectMany(d => GetActiveSources(d));
		}

		/// <summary>
		/// Gets the sources currently routed to the given destination.
		/// </summary>
		/// <param name="destination"></param>
		/// <returns></returns>
		private IEnumerable<ISource> GetActiveSources(IDestination destination)
		{
			if (destination == null)
				throw new ArgumentNullException("destination");

			if (m_SubscribedRoutingGraph == null)
				return Enumerable.Empty<ISource>();

			return m_SubscribedRoutingGraph.GetActiveSourceEndpoints(destination,
			                                                         destination.ConnectionType, false, true)
			                               .Select(e => GetSourceFromEndpoint(e))
			                               .Where(s => s != null);
		}

		/// <summary>
		/// Gets the source for the given endpoint info.
		/// </summary>
		/// <param name="endpoint"></param>
		/// <returns></returns>
		[CanBeNull]
		private ISource GetSourceFromEndpoint(EndpointInfo endpoint)
		{
			return m_SubscribedRoutingGraph == null
				       ? null
				       : m_SubscribedRoutingGraph.Sources.FirstOrDefault(s => s.Contains(endpoint));
		}

		/// <summary>
		/// Gets the destinations for the current room.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IDestination> GetRoomDestinations()
		{
			return Originators.GetInstancesRecursive<IDestination>();
		}

		/// <summary>
		/// Looks up the current actively routed sources and caches them.
		/// </summary>
		private void UpdateCachedActiveSources()
		{
			Log(eSeverity.Informational, "{0} updating active source cache", this);

			IcdHashSet<ISource> active = GetActiveRoomSources().ToIcdHashSet();

			bool change = active.NonIntersection(m_CachedActiveSources).Any();
			if (!change)
				return;

			m_CachedActiveSources.Clear();
			m_CachedActiveSources.AddRange(active);

			Log(eSeverity.Informational, "{0} active sources changed", this);
			OnActiveSourcesChange.Raise(this);
		}

		#endregion

		#region RoutingGraph Callbacks

		/// <summary>
		/// Subscribe to the routing graph events.
		/// </summary>
		/// <param name="routingGraph"></param>
		private void Subscribe(IRoutingGraph routingGraph)
		{
			m_SubscribedRoutingGraph = routingGraph;

			if (routingGraph == null)
				return;

			routingGraph.OnRouteChanged += RoutingGraphOnRouteChanged;
		}

		/// <summary>
		/// Unsubscribe from the routing graph events.
		/// </summary>
		/// <param name="routingGraph"></param>
		private void Unsubscribe(IRoutingGraph routingGraph)
		{
			if (routingGraph == null)
				return;

			routingGraph.OnRouteChanged -= RoutingGraphOnRouteChanged;
		}

		/// <summary>
		/// Called when the routing changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void RoutingGraphOnRouteChanged(object sender, EventArgs eventArgs)
		{
			UpdateCachedActiveSources();
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_CrosspointsSection.Execute(() => m_Crosspoints.Clear());

			Unsubscribe(m_SubscribedRoutingGraph);
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SimplRoomSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.SetCrosspoints(GetCrosspoints());
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(SimplRoomSettings settings, IDeviceFactory factory)
		{
			// Ensure the routing graph loads first
			IRoutingGraph graph = factory.GetOriginators<IRoutingGraph>().FirstOrDefault();

			base.ApplySettingsFinal(settings, factory);

			SetCrosspoints(settings.GetCrosspoints());

			Subscribe(graph);
			UpdateCachedActiveSources();
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("PrintCrosspoints", "Prints the crosspoints added to the room", () => PrintCrosspoints());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Returns a table of the crosspoints in the room.
		/// </summary>
		/// <returns></returns>
		private string PrintCrosspoints()
		{
			m_CrosspointsSection.Enter();

			try
			{
				TableBuilder builder = new TableBuilder("Id", "Type");

				foreach (KeyValuePair<ushort, eCrosspointType> kvp in m_Crosspoints)
					builder.AddRow(kvp.Key, kvp.Value);

				return builder.ToString();
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}
		}

		#endregion
	}
}
