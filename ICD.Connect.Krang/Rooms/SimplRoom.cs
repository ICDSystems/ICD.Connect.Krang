using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Endpoints;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Rooms
{
	public sealed class SimplRoom : AbstractRoom<SimplRoomSettings>
	{
		public enum eCrosspointType
		{
			None,
			Lighting,
			Hvac
		}

		public event EventHandler OnVolumeLevelSet;
		public event EventHandler OnVolumeLevelRamp;
		public event EventHandler OnVolumeLevelFeedbackChange;
		public event EventHandler OnVolumeMuteFeedbackChange;
		public event EventHandler OnActiveSourcesChange;

		private readonly Dictionary<ushort, eCrosspointType> m_Crosspoints;
		private readonly SafeCriticalSection m_CrosspointsSection;

		private readonly IcdHashSet<ISource> m_CachedActiveSources;

		private ushort m_VolumeLevelFeedback;
		private bool m_VolumeMuteFeedback;
		private IRoutingGraph m_SubscribedRoutingGraph;

		#region Properties

		public ushort VolumeLevelFeedback
		{
			get { return m_VolumeLevelFeedback; }
			private set
			{
				if (value == m_VolumeLevelFeedback)
					return;

				m_VolumeLevelFeedback = value;

				OnVolumeLevelFeedbackChange.Raise(this);
			}
		}

		public bool VolumeMuteFeedback
		{
			get { return m_VolumeMuteFeedback; }
			private set
			{
				if (value == m_VolumeMuteFeedback)
					return;

				m_VolumeMuteFeedback = value;

				OnVolumeMuteFeedbackChange.Raise(this);
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
			OnVolumeLevelSet = null;
			OnVolumeLevelRamp = null;
			OnVolumeLevelFeedbackChange = null;
			OnVolumeMuteFeedbackChange = null;
			OnActiveSourcesChange = null;

			m_CachedActiveSources.Clear();

			base.DisposeFinal(disposing);
		}

		#region Methods

		public void SetVolumeLevel(ushort volume)
		{
			OnVolumeLevelSet.Raise(this);
		}

		public void SetVolumeFeedback(ushort volume)
		{
			VolumeLevelFeedback = volume;
		}

		public void SetMuteFeedback(bool mute)
		{
			VolumeMuteFeedback = mute;
		}

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
		/// Routes the source with the given id to all destinations in the current room.
		/// Unroutes if no source found with the given id.
		/// </summary>
		/// <param name="sourceId"></param>
		public void SetSource(ushort sourceId)
		{
			ISource source = GetSource(sourceId);
			if (source == null)
				Unroute();
			else
				Route(source);
		}

		/// <summary>
		/// Gets the current, actively routed source.
		/// </summary>
		[CanBeNull]
		public ISource GetSource()
		{
			return m_CachedActiveSources.OrderBy(s => s.Id).FirstOrDefault();
		}

		#endregion

		#region Routing

		/// <summary>
		/// Routes the source to the destination.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		private void Route(ISource source, IDestination destination)
		{
			if (m_SubscribedRoutingGraph == null)
				return;

			eConnectionType connectionType = EnumUtils.GetFlagsIntersection(source.ConnectionType, destination.ConnectionType);
			m_SubscribedRoutingGraph.Route(source.Endpoint, destination.Endpoint, connectionType, Id);
		}

		/// <summary>
		/// Routes the source to all destinations in the current room.
		/// </summary>
		/// <param name="source"></param>
		private void Route(ISource source)
		{
			GetRoomDestinations().ForEach(d => Route(source, d));
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

			eConnectionType connectionType = EnumUtils.GetFlagsIntersection(source.ConnectionType, destination.ConnectionType);
			m_SubscribedRoutingGraph.Unroute(source.Endpoint, destination.Endpoint, connectionType, Id);
		}

		/// <summary>
		/// Unroutes all sources from the destination.
		/// </summary>
		/// <param name="destination"></param>
		private void Unroute(IDestination destination)
		{
			GetActiveSources(destination).ForEach(s => Unroute(s, destination));
		}

		/// <summary>
		/// Unroutes all the destinations in the room.
		/// </summary>
		private void Unroute()
		{
			GetRoomDestinations().ForEach(Unroute);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the source with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[CanBeNull]
		private ISource GetSource(ushort id)
		{
			if (m_SubscribedRoutingGraph == null)
				return null;

			ISource source;
			m_SubscribedRoutingGraph.Sources.TryGetChild(id, out source);
			return source;
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

			return m_SubscribedRoutingGraph.GetActiveSourceEndpoints(destination.Endpoint,
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
				       : m_SubscribedRoutingGraph.Sources.FirstOrDefault(s => s.Endpoint == endpoint);
		}

		/// <summary>
		/// Gets the destinations for the current room.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IDestination> GetRoomDestinations()
		{
			return Originators.GetInstancesRecursive<IDestination>();
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
			IcdHashSet<ISource> active = GetActiveRoomSources().ToIcdHashSet();

			bool change = active.NonIntersection(m_CachedActiveSources).Any();
			if (!change)
				return;

			m_CachedActiveSources.Clear();
			m_CachedActiveSources.AddRange(active);

			OnActiveSourcesChange.Raise(this);
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
			base.ApplySettingsFinal(settings, factory);

			SetCrosspoints(settings.GetCrosspoints());
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Volume Level", m_VolumeLevelFeedback);
			addRow("Volume Mute", m_VolumeMuteFeedback);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("PrintCrosspoints", "Prints the crosspoints added to the room", () => PrintCrosspoints());
			yield return new GenericConsoleCommand<ushort>("SetSource", "SetSource <Source Id>", i => SetSource(i));
			yield return new GenericConsoleCommand<ushort>("SetVolumeLevel", "SetVolumeLevel <Level>", l => SetVolumeLevel(l));
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
