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
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Extensions;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Destinations;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup;
using ICD.Connect.Krang.SPlus.Themes;
using ICD.Connect.Krang.SPlus.VolumePoints;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.Endpoints;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Routing.PathFinding;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Settings.Originators;
using ICD.Connect.Settings.Originators.Simpl;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.SPlus.Rooms
{
	[Flags]
	public enum eSourceTypeRouted
	{
		None = 0,
		Video = 1,
		Audio = 2,
		AudioVideo = Audio | Video
	}

	public enum eCrosspointType
	{
		None = 0,
		Lighting = 1,
		Hvac = 2
	}

	public sealed class SimplRoom : AbstractRoom<SimplRoomSettings>, IKrangAtHomeRoom, ISimplOriginator
	{
		#region Events

		public event EventHandler<RequestShimResyncEventArgs> OnRequestShimResync; 

		public event EventHandler OnActiveSourcesChange;

		public event EventHandler<GenericEventArgs<IVolumeDeviceControl>> OnActiveVolumeControlChanged; 

		#endregion

		#region Fields

		private readonly Dictionary<eCrosspointType, ushort> m_Crosspoints;
		private readonly SafeCriticalSection m_CrosspointsSection;

		private readonly IcdHashSet<ISource> m_CachedActiveSources;
		private DateTime m_CachedActiveSourcesUpdateTime;

		private IRoutingGraph m_SubscribedRoutingGraph;

		private IPathFinder m_PathFinder;

		private IVolumeDeviceControl m_ActiveVolumeControl;

		#endregion

		#region Properties

		private IPathFinder PathFinder
		{
			get { return m_PathFinder = m_PathFinder ?? new DefaultPathFinder(m_SubscribedRoutingGraph, Id); }
		}

		public eSourceTypeRouted SourceType { get; private set; }

		public IVolumeDeviceControl ActiveVolumeControl
		{
			get { return m_ActiveVolumeControl; }
			private set
			{
				if (value == m_ActiveVolumeControl)
					return;

				m_ActiveVolumeControl = value;

				OnActiveVolumeControlChanged.Raise(this, new GenericEventArgs<IVolumeDeviceControl>(m_ActiveVolumeControl));

			}
		}

		public KrangAtHomeRouting Routing { get; private set; }

		public KrangAtHomeSourceGroupManager SourceGroupManager { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public SimplRoom()
		{
			m_Crosspoints = new Dictionary<eCrosspointType, ushort>();
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
		public IEnumerable<KeyValuePair<eCrosspointType, ushort>> GetCrosspoints()
		{
			return m_CrosspointsSection.Execute(() => m_Crosspoints.OrderByKey().ToArray());
		}

		public int GetCrosspointsCount()
		{
			return m_CrosspointsSection.Execute(() => m_Crosspoints.Count);
		}

		public void ApplyTheme(KrangAtHomeTheme theme)
		{
			Routing = theme.KrangAtHomeRouting;
			SourceGroupManager = theme.KrangAtHomeSourceGroupManager;

		}

		/// <summary>
		/// Sets the crosspoints.
		/// </summary>
		/// <param name="crosspoints"></param>
		public void SetCrosspoints(IEnumerable<KeyValuePair<eCrosspointType, ushort>> crosspoints)
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
		public IKrangAtHomeSource GetSource()
		{
			ISource source = m_CachedActiveSources.OrderBy(s => s.Id).FirstOrDefault();

			IKrangAtHomeSource krangAtHomeSource = source as IKrangAtHomeSource;

			return krangAtHomeSource;
		}

		public IEnumerable<IKrangAtHomeSourceBase> GetSourcesBase()
		{
			IKrangAtHomeSource source = GetSource();

			IcdHashSet<IKrangAtHomeSourceBase> sourcesAndGroups = new IcdHashSet<IKrangAtHomeSourceBase>();

			if (source != null)
			{
				sourcesAndGroups.Add(source);
				sourcesAndGroups.AddRange(SourceGroupManager.GetSourceGroupsForSource(source).OfType<IKrangAtHomeSourceBase>());
			}

			return sourcesAndGroups;
		}

		public void SetSource(IKrangAtHomeSourceBase source, eSourceTypeRouted type)
		{
			Route(source, type);
		}

		public void SetSourceId(int sourceId, eSourceTypeRouted type)
		{
			IKrangAtHomeSourceBase source = GetSourceId(sourceId);

			SetSource(source, type);

		}

		#endregion

		#region Routing

		/// <summary>
		/// Routes the source to all destinations in the current room.
		/// </summary>
		/// <param name="sourceBase"></param>
		/// <param name="sourceType"></param>
		private void Route(IKrangAtHomeSourceBase sourceBase, eSourceTypeRouted sourceType)
		{
			Log(eSeverity.Debug, "Routing SourceBase:{0} Type:{1}", sourceBase, sourceType);

			if (sourceBase == null)
			{
				Unroute();
				return;
			}

			if (m_SubscribedRoutingGraph == null)
				return;

			IKrangAtHomeSource source = SourceGroupManager.GetAndAssignSourceForRoom(this, sourceBase);

			if (source == null)
			{
				Log(eSeverity.Error, "Couldn't get source for base {0}", sourceBase);
				return;
			}

			IKrangAtHomeDestination[] videoDestinations = GetRoomDestinations(eConnectionType.Video).ToArray();
			IKrangAtHomeDestination[] audioDestinations = GetRoomDestinations(eConnectionType.Audio).ToArray();

			IcdHashSet<ConnectionPath> paths = new IcdHashSet<ConnectionPath>();

			// Route Video
			// If the source is a video source, and it's routed as a video source, make the route
			if (source.ConnectionType.HasFlag(eConnectionType.Video) && sourceType.HasFlag(eSourceTypeRouted.Video))
			{
				paths.AddRange(
				               PathBuilder.FindPaths()
				                          .From(source)
				                          .To(videoDestinations)
				                          .OfType(eConnectionType.Video)
				                          .With(PathFinder));
			}
			else
			{
				// Unroute and power off all video destinations if we aren't routing anything there
				videoDestinations.ForEach(destination => Unroute(destination, eConnectionType.Video));
				videoDestinations.Where(d => d.ConnectionType == eConnectionType.Video).Select(d => d.Device).ForEach(PowerOffDestinationDevice);
				
			}

			// Route Audio
			// If the source is an audio sourc, and it's routed as an audio source, make the route
			if (source.ConnectionType.HasFlag(eConnectionType.Audio) && sourceType.HasFlag(eSourceTypeRouted.Audio))
			{
				// Figure out what audio options are needed
				eAudioOption audioOption = sourceType == eSourceTypeRouted.AudioVideo
					                           ? eAudioOption.AudioVideoOnly
					                           : eAudioOption.AudioOnly;

				// Get Used vs Unused audio destinations
				var audioDestinationsUsed = audioDestinations.Where(d => d.AudioOption.HasFlag(audioOption)).ToArray();
				var audioDestinationsUnused = audioDestinations.Where(d => !d.AudioOption.HasFlag(audioOption)).ToArray();

				// Route to used destinations
				paths.AddRange(
				               PathBuilder.FindPaths()
				                          .From(source)
				                          .To(audioDestinationsUsed)
				                          .OfType(eConnectionType.Audio)
				                          .With(PathFinder));

				// Unroute to unused destinations
				audioDestinationsUnused.ForEach(destination => Unroute(destination,eConnectionType.Audio));

				// Set Volume Control
				try
				{
					KrangAtHomeVolumePoint volumePoint =
						Originators.GetInstanceRecursive<KrangAtHomeVolumePoint>(p => p.AudioOption.HasFlag(audioOption));
					if (volumePoint != null)
					{
						IVolumeDeviceControl volumeControl = Core.GetControl<IVolumeDeviceControl>(volumePoint.DeviceId,
						                                                                           volumePoint.ControlId);
						ActiveVolumeControl = volumeControl;
					}
					else
					{
						Log(eSeverity.Alert, "No volume point found that matches state");
						ActiveVolumeControl = null;
					}
					
				}
				catch (InvalidCastException ex)
				{
					Log(eSeverity.Error, "Unable to get volume control:{0}", ex.Message);
					ActiveVolumeControl = null;
				}
				catch (InvalidOperationException ex)
				{
					Log(eSeverity.Error, "Unable to get volume control:{0}", ex.Message);
					ActiveVolumeControl = null;
				}
				catch (ArgumentException ex)
				{
					Log(eSeverity.Error, "Unable to get volume control:{0}", ex.Message);
					ActiveVolumeControl = null;
				}
			}
			else
			{
				// Unroute all audio destinations if we aren't routing anything there
				audioDestinations.ForEach(destination => Unroute(destination, eConnectionType.Audio));

				// Set null volume control
				ActiveVolumeControl = null;
			}

			//Todo: handle lack of path approprately

			// Power On + Input Switch Destinations
			foreach (ConnectionPath path in paths)
			{
				PowerOnDestinationDevice(path.DestinationEndpoint);
				
				SetActiveInputDestinationDevice(path.DestinationEndpoint, eConnectionType.Audio | eConnectionType.Video);
			}

			// Update Cached Source to this source
			UpdateCachedActiveSourceFake(source);

			Log(eSeverity.Informational, "Routing {0}", source);

			m_SubscribedRoutingGraph.RoutePaths(paths, Id);
			SourceType = sourceType;
		}

		/// <summary>
		/// Unroutes all the destinations in the room.
		/// </summary>
		private void Unroute()
		{
			Log(eSeverity.Informational, "Unrouting all");

			GetRoomDestinations().ForEach(Unroute);
			SourceGroupManager.ClearRoomAssignedSources(this);

			// Update Cached Source to this source
			UpdateCachedActiveSourceFake(null);
		}

		/// <summary>
		/// Unroutes all sources from the destination.
		/// </summary>
		/// <param name="destination"></param>
		private void Unroute(IDestination destination)
		{
			Log(eSeverity.Informational, "Unrouting {0}", destination);

			GetActiveSources(destination).ForEach(s => Unroute(s, destination));

			PowerOffDestinationDevice(destination.Device);
		}

		private void Unroute(IDestination destination, eConnectionType connections)
		{
			Log(eSeverity.Informational, "Unrouting {0}", destination);

			GetActiveSources(destination).ForEach(s => Unroute(s, destination, connections));
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

			Log(eSeverity.Debug, "Unrouting {0} from {1}", source, destination);

			eConnectionType connectionType = EnumUtils.GetFlagsIntersection(source.ConnectionType, destination.ConnectionType);
			m_SubscribedRoutingGraph.Unroute(source, destination, connectionType, Id);
		}

		private void Unroute(ISource source, IDestination destination, eConnectionType connections)
		{
			if (m_SubscribedRoutingGraph == null)
				return;

			Log(eSeverity.Debug, "Unrouting {0} from {1} for connections {2}", source, destination, connections);

			eConnectionType connectionType = EnumUtils.GetFlagsIntersection(source.ConnectionType, destination.ConnectionType, connections);
			m_SubscribedRoutingGraph.Unroute(source, destination, connectionType, Id);
		}

		#endregion

		#region Destinations

		private void PowerOnDestinationDevice(EndpointInfo destination)
		{
			IDeviceBase originator = Core.Originators.GetChild(destination.Device) as IDeviceBase;
			if (originator == null)
				return;

			IPowerDeviceControl powerControl = originator.Controls.GetControl<IPowerDeviceControl>();

			if (powerControl != null)
				powerControl.PowerOn();
		}

		private void PowerOffDestinationDevice(int destinationId)
		{
			IDeviceBase originator = Core.Originators.GetChild(destinationId) as IDeviceBase;
			if (originator == null)
				return;

			PowerOffDestinationDevice(originator);
		}

		private void PowerOffDestinationDevice(IDeviceBase destinationDevice)
		{
			IPowerDeviceControl powerControl = destinationDevice.Controls.GetControl<IPowerDeviceControl>();

			if (powerControl != null)
				powerControl.PowerOff();
		}

		private void SetActiveInputDestinationDevice(EndpointInfo destination, eConnectionType connectionType)
		{
			IDeviceBase originator = Core.Originators.GetChild(destination.Device) as IDeviceBase;
			if (originator == null)
				return;

			IRouteInputSelectControl routeControl = originator.Controls.GetControl<IRouteInputSelectControl>();

			if (routeControl != null)
				routeControl.SetActiveInput(destination.Address, connectionType);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the source with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[CanBeNull]
		public IKrangAtHomeSourceBase GetSourceId(int id)
		{
			if (m_SubscribedRoutingGraph == null)
				return null;

			IOriginator source;

			Core.Originators.TryGetChild(id, out source);
			return source as IKrangAtHomeSourceBase;
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
		private IEnumerable<IKrangAtHomeDestination> GetRoomDestinations()
		{
			return Originators.GetInstancesRecursive<IKrangAtHomeDestination>();
		}

		private IEnumerable<IKrangAtHomeDestination> GetRoomDestinations(eConnectionType connectionType)
		{
			return
				Originators.GetInstancesRecursive<IKrangAtHomeDestination>().Where(d => d.ConnectionType.HasFlags(connectionType));
		}

		private void UpdateCachedActiveSourcesIfExpired()
		{
			if (m_CachedActiveSourcesUpdateTime.AddSeconds(30) < DateTime.UtcNow)
				UpdateCachedActiveSources();
		}

		/// <summary>
		/// Looks up the current actively routed sources and caches them.
		/// </summary>
		private void UpdateCachedActiveSources()
		{
			//Log(eSeverity.Informational, "{0} updating active source cache", this);

			IcdHashSet<ISource> active = GetActiveRoomSources().ToIcdHashSet();

			bool change = active.NonIntersection(m_CachedActiveSources).Any();
			if (!change)
				return;

			m_CachedActiveSources.Clear();
			m_CachedActiveSources.AddRange(active);

			//Log(eSeverity.Informational, "{0} active sources changed", this);
			OnActiveSourcesChange.Raise(this);
		}

		/// <summary>
		/// Updates the source with fake feedback :(
		/// </summary>
		/// <param name="source"></param>
		private void UpdateCachedActiveSourceFake(ISource source)
		{
			IcdHashSet<ISource> active = new IcdHashSet<ISource>();
			if (source != null)
				active.Add(source);

			bool change = active.NonIntersection(m_CachedActiveSources).Any();
			if (!change)
				return;

			m_CachedActiveSourcesUpdateTime = DateTime.UtcNow;

			m_CachedActiveSources.Clear();
			m_CachedActiveSources.AddRange(active);

			//Log(eSeverity.Informational, "{0} active sources changed", this);
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

			routingGraph.RoutingCache.OnSourceDestinationRouteChanged += RoutingCacheOnSourceDestinationRouteChanged;
			routingGraph.RoutingCache.OnDestinationEndpointActiveChanged += RoutingCacheOnDestinationEndpointActiveChanged;
		}

		/// <summary>
		/// Unsubscribe from the routing graph events.
		/// </summary>
		/// <param name="routingGraph"></param>
		private void Unsubscribe(IRoutingGraph routingGraph)
		{
			if (routingGraph == null)
				return;

			routingGraph.RoutingCache.OnSourceDestinationRouteChanged -= RoutingCacheOnSourceDestinationRouteChanged;
			routingGraph.RoutingCache.OnDestinationEndpointActiveChanged -= RoutingCacheOnDestinationEndpointActiveChanged;
		}

		private void RoutingCacheOnDestinationEndpointActiveChanged(object sender, CacheStateChangedEventArgs args)
		{
			// Filter by destinations in this room
			if (args.Endpoints.AnyAndAll(e => GetRoomDestinations().AnyAndAll(d => d.Contains(e))))
				UpdateCachedActiveSourcesIfExpired();
		}

		private void RoutingCacheOnSourceDestinationRouteChanged(object sender, SourceDestinationRouteChangedEventArgs args)
		{
			UpdateCachedActiveSourcesIfExpired();
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
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);
			addRow("Source", GetSource());
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<int, eSourceTypeRouted>("Route", "Route (sourceid) (Audio|Video|AudioVideo)", (source, type) => SetSourceId(source, type));
			yield return new ConsoleCommand("Unroute", "Unroutes all the destinations in the room", () => Unroute());
			yield return new ConsoleCommand("UpdateCachedActiveSources", "Recaculates cached active sources", () => UpdateCachedActiveSources());
			yield return new ConsoleCommand("GetActiveInputs", "Gets the active inputs for the room", () => GetActiveRoomSources());
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
				TableBuilder builder = new TableBuilder("Type", "CrosspointId");

				foreach (KeyValuePair<eCrosspointType, ushort> kvp in m_Crosspoints)
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
