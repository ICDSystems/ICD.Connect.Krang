using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Extensions;
using ICD.Connect.Krang.Routing.Connections;
using ICD.Connect.Krang.Routing.ConnectionUsage;
using ICD.Connect.Krang.Routing.StaticRoutes;
using ICD.Connect.Krang.Settings;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.ConnectionUsage;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.Endpoints;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Endpoints.Groups;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.Extensions;
using ICD.Connect.Routing.StaticRoutes;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Routing
{
	/// <summary>
	/// Maps devices to each other via connections.
	/// </summary>
	[PublicAPI]
	public sealed class RoutingGraph : AbstractOriginator<RoutingGraphSettings>, IConsoleNode, IRoutingGraph
	{
		private readonly IcdHashSet<IRouteSwitcherControl> m_SubscribedSwitchers;

		private readonly ConnectionsCollection m_Connections;
		private readonly StaticRoutesCollection m_StaticRoutes;
		private readonly ConnectionUsageCollection m_ConnectionUsages;
		private readonly CoreSourceCollection m_Sources;
		private readonly CoreDestinationCollection m_Destinations;
		private readonly CoreDestinationGroupCollection m_DestinationGroups;

		private readonly SafeCriticalSection m_PendingRoutesSection;
		private readonly Dictionary<Guid, int> m_PendingRoutes;

		#region Events

		/// <summary>
		/// Raised when a route operation fails or succeeds.
		/// </summary>
		public event EventHandler<RouteFinishedEventArgs> OnRouteFinished;

		/// <summary>
		/// Raised when a switcher changes routing.
		/// </summary>
		public event EventHandler OnRouteChanged;

		#endregion

		#region Properties

		public ConnectionsCollection Connections { get { return m_Connections; } }

		IConnectionsCollection IRoutingGraph.Connections { get { return Connections; } }

		public StaticRoutesCollection StaticRoutes { get { return m_StaticRoutes; } }

		IStaticRoutesCollection IRoutingGraph.StaticRoutes { get { return StaticRoutes; } }

		public ConnectionUsageCollection ConnectionUsages { get { return m_ConnectionUsages; } }

		IConnectionUsageCollection IRoutingGraph.ConnectionUsages { get { return ConnectionUsages; } }

		public CoreSourceCollection Sources { get { return m_Sources; } }

		IOriginatorCollection<ISource> IRoutingGraph.Sources { get { return Sources; } }

		public CoreDestinationCollection Destinations { get { return m_Destinations; } }

		IOriginatorCollection<IDestination> IRoutingGraph.Destinations { get { return Destinations; } }

		public CoreDestinationGroupCollection DestinationGroups { get { return m_DestinationGroups; } }

		IOriginatorCollection<IDestinationGroup> IRoutingGraph.DestinationGroups { get { return DestinationGroups; } }

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return "RoutingGraph"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Maps the routing of device outputs to inputs."; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public RoutingGraph()
		{
			m_SubscribedSwitchers = new IcdHashSet<IRouteSwitcherControl>();

			m_StaticRoutes = new StaticRoutesCollection(this);
			m_ConnectionUsages = new ConnectionUsageCollection(this);
			m_Connections = new ConnectionsCollection(this);
			m_Sources = new CoreSourceCollection();
			m_Destinations = new CoreDestinationCollection();
			m_DestinationGroups = new CoreDestinationGroupCollection();

			m_PendingRoutes = new Dictionary<Guid, int>();
			m_PendingRoutesSection = new SafeCriticalSection();
			ServiceProvider.AddService<IRoutingGraph>(this);
		}

		/// <summary>
		/// Called when connections are added or removed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ConnectionsOnConnectionsChanged(object sender, EventArgs eventArgs)
		{
			ConnectionUsages.RemoveInvalid();

			SubscribeSwitchers();
			StaticRoutes.UpdateStaticRoutes();
		}

		#endregion

		/// <summary>
		/// Clears the RoutingGraph of any connections and static routes.
		/// </summary>
		public void Clear()
		{
			StaticRoutes.Clear();
			Connections.Clear();
		}

		#region Recursion

		/// <summary>
		/// Finds the actively routed sources for the destination at the given input address.
		/// Will return multiple items when connection types are combined, e.g. seperate audio and video sources.
		/// </summary>
		/// <param name="destinationInput"></param>
		/// <param name="type"></param>
		/// <param name="signalDetected">When true skips inputs where no video is detected.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <returns>The sources</returns>
		public IEnumerable<EndpointInfo> GetActiveSourceEndpoints(EndpointInfo destinationInput, eConnectionType type,
		                                                          bool signalDetected)
		{
			IRouteDestinationControl destinationControl = this.GetDestinationControl(destinationInput);
			if (signalDetected && !destinationControl.GetSignalDetectedState(destinationInput.Address, type))
				yield break;

			Connection inputConnection = Connections.GetInputConnection(destinationControl, destinationInput.Address, type);
			if (inputConnection == null)
				yield break;

			IRouteSourceControl sourceControl = this.GetSourceControl(inputConnection);
			if (sourceControl == null)
				yield break;

			IRouteMidpointControl sourceAsSwitcher = sourceControl as IRouteMidpointControl;

			// If the source is a midpoint, loop through the routed inputs
			if (sourceAsSwitcher != null)
			{
				foreach (ConnectorInfo info in sourceAsSwitcher.GetInputs(inputConnection.Source.Address, type))
				{
					foreach (EndpointInfo result in GetActiveSourceEndpoints(sourceAsSwitcher.GetInputEndpointInfo(info.Address), info.ConnectionType, signalDetected))
						yield return result;
				}
				yield break;
			}

			// The source is a source!
			yield return sourceControl.GetOutputEndpointInfo(inputConnection.Source.Address);
		}

		/// <summary>
		/// Finds the destinations that the source is actively routed to.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="sourceOutput"></param>
		/// <param name="type"></param>
		/// <param name="signalDetected">When true skips inputs where no video is detected.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <returns>The sources</returns>
		public IEnumerable<EndpointInfo> GetActiveDestinationEndpoints(IRouteSourceControl sourceControl, int sourceOutput,
		                                                               eConnectionType type, bool signalDetected)
		{
			if (sourceControl == null)
				throw new ArgumentNullException("sourceControl");

			return FindActivePaths(sourceControl.GetOutputEndpointInfo(sourceOutput), type, signalDetected)
				.Select(p => p.Last().Destination);
		}

		/// <summary>
		/// Recurses over all of the source devices that can be routed to the destination.
		/// </summary>
		/// <param name="destinationControl"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<IRouteSourceControl> GetSourceControlsRecursive(IRouteDestinationControl destinationControl,
		                                                                   eConnectionType type)
		{
			if (destinationControl == null)
				throw new ArgumentNullException("destinationControl");

			Queue<IRouteSourceControl> sources = new Queue<IRouteSourceControl>();
			sources.EnqueueRange(GetSourceControls(destinationControl, type));

			while (sources.Count > 0)
			{
				IRouteSourceControl source = sources.Dequeue();
				if (source == null)
					continue;

				yield return source;

				IRouteDestinationControl sourceAsDestination = source as IRouteDestinationControl;
				if (sourceAsDestination != null)
					sources.EnqueueRange(GetSourceControls(sourceAsDestination, type));
			}
		}

		/// <summary>
		/// Simple check to see if the source is detected by the next node in the graph.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		[PublicAPI]
		public bool SourceDetected(IRouteSourceControl sourceControl, eConnectionType type)
		{
			if (sourceControl == null)
				throw new ArgumentNullException("sourceControl");

			return Connections.GetOutputs(sourceControl, type).Any(o => SourceDetected(sourceControl, o, type));
		}

		/// <summary>
		/// Returns true if the source control is detected by the next node in the graph at the given output.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="output"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		[PublicAPI]
		public bool SourceDetected(IRouteSourceControl sourceControl, int output, eConnectionType type)
		{
			if (sourceControl == null)
				throw new ArgumentNullException("sourceControl");

			int input;
			IRouteDestinationControl destination = GetDestinationControl(sourceControl, output, type, out input);
			return destination != null && destination.GetSignalDetectedState(input, type);
		}

		/// <summary>
		/// Enumerates through possible paths from the source to the destination.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		/// <param name="type"></param>
		/// <param name="roomId"></param>
		private IEnumerable<List<Connection>> FindPaths(EndpointInfo source, EndpointInfo destination, eConnectionType type,
		                                                int roomId)
		{
			Queue<EndpointInfo> queue = new Queue<EndpointInfo>();
			queue.Enqueue(source);

			Dictionary<IRouteDestinationControl, List<Connection>> paths =
				new Dictionary<IRouteDestinationControl, List<Connection>>();

			while (queue.Any())
			{
				EndpointInfo currentSource = queue.Dequeue();

				IEnumerable<Connection> availableConnections =
					Connections.GetOutputConnections(currentSource.Device, currentSource.Control, type)
					           .Where(c =>
					                  ConnectionUsages.CanRouteConnection(c, source, roomId, type) &&
					                  c.IsAvailableToSourceDevice(source.Device) &&
					                  c.IsAvailableToRoom(roomId) &&
					                  (!(currentSource.Device == source.Device && currentSource.Control == source.Control) ||
					                   source.Address == c.Source.Address));

				foreach (Connection connection in availableConnections)
				{
					IRouteDestinationControl destinationControl = this.GetDestinationControl(connection);
					if (paths.ContainsKey(destinationControl))
						continue;

					// If the current source is the root, only consider the desired source address
					if (currentSource.Device == source.Device &&
					    currentSource.Control == source.Control &&
					    connection.Source.Address != source.Address)
						continue;

					IRouteDestinationControl cast = this.GetSourceControl(connection) as IRouteDestinationControl;
					List<Connection> path = cast != null && paths.ContainsKey(cast)
						                        ? new List<Connection>(paths[cast])
						                        : new List<Connection>();

					path.Add(connection);

					if (connection.Destination == destination)
						yield return path;

					paths[destinationControl] = path;

					IRouteSourceControl control = destinationControl as IRouteSourceControl;
					if (control != null)
						queue.Enqueue(control.GetOutputEndpointInfo(connection.Source.Address));
				}
			}
		}

		/// <summary>
		/// Finds the current paths from the given source to the destination.
		/// Return multiple paths if multiple connection types are provided.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		/// <param name="type"></param>
		/// <param name="signalDetected"></param>
		/// <returns></returns>
		public IEnumerable<Connection[]> FindActivePaths(EndpointInfo source, EndpointInfo destination, eConnectionType type,
		                                                 bool signalDetected)
		{
			foreach (Connection[] path in FindActivePaths(source, type, signalDetected))
			{
				// It's possible the path goes through our destination
				int index = path.FindIndex(c => c.Destination == destination);
				if (index < 0)
					continue;

				yield return path.Take(index + 1).ToArray();
			}
		}

		/// <summary>
		/// Finds all of the active paths from the given source.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="type"></param>
		/// <param name="signalDetected"></param>
		/// <returns></returns>
		public IEnumerable<Connection[]> FindActivePaths(EndpointInfo source, eConnectionType type, bool signalDetected)
		{
			return EnumUtils.HasMultipleFlags(type)
				       ? EnumUtils.GetFlagsExceptNone(type).SelectMany(f => FindActivePaths(source, f, signalDetected))
				       : FindActivePathsSingleFlag(source, type, signalDetected);
		}

		/// <summary>
		/// Finds all of the active paths from the given source.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="type"></param>
		/// <param name="signalDetected"></param>
		/// <returns></returns>
		private IEnumerable<Connection[]> FindActivePathsSingleFlag(EndpointInfo source, eConnectionType type,
		                                                            bool signalDetected)
		{
			return FindActivePathsSingleFlag(source, type, signalDetected, new List<Connection>()).ToArray();
		}

		/// <summary>
		/// Finds all of the active paths from the given source.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="type"></param>
		/// <param name="signalDetected"></param>
		/// <param name="visited"></param>
		/// <returns></returns>
		private IEnumerable<Connection[]> FindActivePathsSingleFlag(EndpointInfo source, eConnectionType type,
		                                                            bool signalDetected, List<Connection> visited)
		{
			if (!EnumUtils.HasSingleFlag(type))
				throw new ArgumentException("Type enum requires exactly 1 flag.", "type");

			// If there is no output connection from this source then we are done.
			Connection outputConnection = Connections.GetOutputConnection(source, type);
			if (outputConnection == null)
			{
				if (visited.Count > 0)
					yield return visited.ToArray();
				yield break;
			}

			// If we care about signal detection state, don't follow this path if the source isn't detected by the destination.
			IRouteDestinationControl destination = this.GetDestinationControl(outputConnection);
			if (signalDetected)
			{
				if (destination == null || !destination.GetSignalDetectedState(outputConnection.Destination.Address, type))
				{
					if (visited.Count > 0)
						yield return visited.ToArray();
					yield break;
				}
			}

			visited.Add(outputConnection);

			// Get the output addresses from the destination if it is a midpoint device.
			IRouteMidpointControl midpoint = destination as IRouteMidpointControl;
			if (midpoint == null)
			{
				if (visited.Count > 0)
					yield return visited.ToArray();
				yield break;
			}

			int[] outputs = midpoint.GetOutputs(outputConnection.Destination.Address, type)
			                        .Select(c => c.Address)
			                        .ToArray();

			if (outputs.Length == 0)
			{
				if (visited.Count > 0)
					yield return visited.ToArray();
				yield break;
			}

			// Recurse for each output.
			foreach (int outputAddress in outputs)
			{
				EndpointInfo newSource = midpoint.GetOutputEndpointInfo(outputAddress);
				foreach (
					Connection[] path in FindActivePathsSingleFlag(newSource, type, signalDetected, new List<Connection>(visited)))
					yield return path;
			}
		}

		#endregion

		#region Routing

		/// <summary>
		/// Configures switchers to route the source to the destination.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="sourceAddress"></param>
		/// <param name="destinationControl"></param>
		/// <param name="destinationAddress"></param>
		/// <param name="type"></param>
		/// <param name="roomId"></param>
		/// <returns>False if route could not be established</returns>
		public bool Route(IRouteSourceControl sourceControl, int sourceAddress, IRouteDestinationControl destinationControl,
		                  int destinationAddress, eConnectionType type, int roomId)
		{
			if (sourceControl == null)
				throw new ArgumentNullException("sourceControl");

			if (destinationControl == null)
				throw new ArgumentNullException("destinationControl");

			EndpointInfo source = sourceControl.GetOutputEndpointInfo(sourceAddress);
			EndpointInfo destination = destinationControl.GetInputEndpointInfo(destinationAddress);

			return Route(source, destination, type, roomId);
		}

		/// <summary>
		/// Routes the source to the destination.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		/// <param name="type"></param>
		/// <param name="roomId"></param>
		/// <returns>False if route could not be established</returns>
		public bool Route(EndpointInfo source, EndpointInfo destination, eConnectionType type, int roomId)
		{
			RouteOperation operation = new RouteOperation
			{
				Id = Guid.NewGuid(),
				Source = source,
				Destination = destination,
				ConnectionType = type,
				RoomId = roomId
			};

			return Route(operation);
		}

		/// <summary>
		/// Configures switchers to establish the given routing operation.
		/// </summary>
		/// <param name="op"></param>>
		/// <returns>False if route could not be established</returns>
		public bool Route(RouteOperation op)
		{
			if (op == null)
				throw new ArgumentNullException("op");

			// Find the first available path
			List<Connection> path =
				FindPaths(op.Source, op.Destination, op.ConnectionType, op.RoomId).FirstOrDefault();
			if (path == null)
				return false;

			int pendingRoutes;
			try
			{
				m_PendingRoutesSection.Enter();
				// Configure the switchers
				for (int i = 0; i < path.Count; i++)
				{
					Connection connection = path[i];
					ConnectionUsages.ClaimConnection(connection, op);

					if (i == path.Count - 1)
						break;
					Connection nextConnection = path[i + 1];

					IRouteSwitcherControl switcher = this.GetDestinationControl(path[i]) as IRouteSwitcherControl;
					if (switcher == null)
						continue;

					op.LocalInput = connection.Destination.Address;
					op.LocalOutput = nextConnection.Source.Address;

					switcher.Route(op);
				}
				pendingRoutes = m_PendingRoutes.ContainsKey(op.Id) ? m_PendingRoutes[op.Id] : 0;
			}
			finally
			{
				m_PendingRoutesSection.Leave();
			}

			if (pendingRoutes > 0)
				return true;
			OnRouteFinished.Raise(this, new RouteFinishedEventArgs(op, true));
			return true;
		}

		/// <summary>
		/// Increments the number of pending routes for the given route operation
		/// </summary>
		/// <param name="op"></param>
		/// <returns></returns>
		public int PendingRouteStarted(RouteOperation op)
		{
			if (op == null)
				throw new ArgumentNullException("op");

			int value;
			try
			{
				m_PendingRoutesSection.Enter();
				if (!m_PendingRoutes.ContainsKey(op.Id))
					m_PendingRoutes[op.Id] = 0;
				value = ++(m_PendingRoutes[op.Id]);
			}
			finally
			{
				m_PendingRoutesSection.Leave();
			}
			return value;
		}

		/// <summary>
		/// Decrements the number of pending routes for the given route operation.
		/// If unsuccessful or all pending routes completed, raises the OnRouteFinished event.
		/// </summary>
		/// <param name="op"></param>
		/// <param name="success"></param>
		/// <returns></returns>
		public int PendingRouteFinished(RouteOperation op, bool success)
		{
			if (op == null)
				throw new ArgumentNullException("op");

			int value = 0;
			try
			{
				m_PendingRoutesSection.Enter();
				if (m_PendingRoutes.ContainsKey(op.Id) && m_PendingRoutes[op.Id] > 0)
				{
					if (!success || m_PendingRoutes[op.Id] == 1)
					{
						m_PendingRoutes.Remove(op.Id);
						OnRouteFinished.Raise(this, new RouteFinishedEventArgs(op, success));
					}
					else
						value = --m_PendingRoutes[op.Id];
				}
			}
			finally
			{
				m_PendingRoutesSection.Leave();
			}
			return value;
		}

		/// <summary>
		/// Searches for switchers currently routing the source and unroutes them.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="sourceAddress"></param>
		/// <param name="type"></param>
		/// <param name="roomId"></param>
		public bool Unroute(IRouteSourceControl sourceControl, int sourceAddress, eConnectionType type, int roomId)
		{
			if (sourceControl == null)
				throw new ArgumentNullException("sourceControl");

			return Unroute(sourceControl.GetOutputEndpointInfo(sourceAddress), type, roomId);
		}

		/// <summary>
		/// Searches for switchers currently routing the source to the destination and unroutes them.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="sourceAddress"></param>
		/// <param name="destinationControl"></param>
		/// <param name="destinationAddress"></param>
		/// <param name="type"></param>
		/// <param name="roomId"></param>
		/// <returns>False if the devices could not be unrouted.</returns>
		public bool Unroute(IRouteSourceControl sourceControl, int sourceAddress, IRouteDestinationControl destinationControl,
		                    int destinationAddress, eConnectionType type, int roomId)
		{
			if (sourceControl == null)
				throw new ArgumentNullException("sourceControl");

			if (destinationControl == null)
				throw new ArgumentNullException("destinationControl");

			return Unroute(sourceControl.GetOutputEndpointInfo(sourceAddress),
			               destinationControl.GetInputEndpointInfo(destinationAddress),
			               type, roomId);
		}

		/// <summary>
		/// Unroutes every path from the given source to the destination.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="sourceAddress"></param>
		/// <param name="destinationControl"></param>
		/// <param name="type"></param>
		/// <param name="roomId"></param>
		/// <returns>False if the devices could not be unrouted.</returns>
		public bool Unroute(IRouteSourceControl sourceControl, int sourceAddress, IRouteDestinationControl destinationControl,
		                    eConnectionType type, int roomId)
		{
			if (sourceControl == null)
				throw new ArgumentNullException("sourceControl");

			if (destinationControl == null)
				throw new ArgumentNullException("destinationControl");

			return Connections.GetInputs(destinationControl, type)
			                  .Select(input => Unroute(sourceControl, sourceAddress, destinationControl, input, type, roomId))
			                  .All(result => result);
		}

		/// <summary>
		/// Unroutes every path from the given source to the destination.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="destinationControl"></param>
		/// <param name="type"></param>
		/// <param name="roomId"></param>
		/// <returns>False if the devices could not be unrouted.</returns>
		public bool Unroute(IRouteSourceControl sourceControl, IRouteDestinationControl destinationControl,
		                    eConnectionType type, int roomId)
		{
			if (sourceControl == null)
				throw new ArgumentNullException("sourceControl");

			if (destinationControl == null)
				throw new ArgumentNullException("destinationControl");

			return Connections.GetOutputs(sourceControl, type)
			                  .Select(output => Unroute(sourceControl, output, destinationControl, type, roomId))
			                  .All(result => result);
		}

		/// <summary>
		/// Searches for switchers currently routing the source and unroutes them.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="type"></param>
		/// <param name="roomId"></param>
		private bool Unroute(EndpointInfo source, eConnectionType type, int roomId)
		{
			bool unrouted = false;

			foreach (Connection[] path in FindActivePaths(source, type, false))
			{
				// Loop backwards looking for switchers closest to the destination
				for (int index = path.Length - 1; index > 0; index--)
				{
					Connection previous = path[index - 1];
					Connection current = path[index];

					if (!Unroute(previous, current, type, roomId))
						break;

					unrouted = true;
				}
			}

			return unrouted;
		}

		/// <summary>
		/// Searches for switchers currently routing the source to the destination and unroutes them.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		/// <param name="type"></param>
		/// <param name="roomId"></param>
		/// <returns>False if the devices could not be unrouted.</returns>
		public bool Unroute(EndpointInfo source, EndpointInfo destination, eConnectionType type, int roomId)
		{
			bool unrouted = false;

			foreach (Connection[] path in FindActivePaths(source, destination, type, false))
			{
				// Loop backwards looking for switchers closest to the destination
				for (int index = path.Length - 1; index > 0; index--)
				{
					Connection previous = path[index - 1];
					Connection current = path[index];

					IRouteMidpointControl midpoint = this.GetSourceControl(current) as IRouteMidpointControl;
					if (midpoint == null)
						continue;

					IRouteSwitcherControl switcher = midpoint as IRouteSwitcherControl;
					if (switcher == null)
						continue;

					if (!Unroute(previous, current, type, roomId))
						break;

					unrouted = true;

					// Stop unrouting if the input is routed to other outputs - we reached a fork
					int input = previous.Destination.Address;
					if (midpoint.GetOutputs(input, type).Any())
						break;
				}
			}

			return unrouted;
		}

		/// <summary>
		/// Unroutes the consecutive connections a -> b.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="type"></param>
		/// <param name="roomId"></param>
		/// <returns>False if unauthorized to unroute the connections</returns>
		private bool Unroute(Connection a, Connection b, eConnectionType type, int roomId)
		{
			if (a == null)
				throw new ArgumentNullException("a");

			if (b == null)
				throw new ArgumentNullException("b");

			if (a.Destination.Device != b.Source.Device || a.Destination.Control != b.Source.Control)
				throw new InvalidOperationException("Connections are not consecutive");

			type = EnumUtils.GetFlagsIntersection(a.ConnectionType, b.ConnectionType, type);

			ConnectionUsageInfo currentUsage = ConnectionUsages.GetConnectionUsageInfo(b);
			if (!currentUsage.CanRoute(roomId, type))
				return false;

			// Remove from usages
			ConnectionUsageInfo previousUsage = ConnectionUsages.GetConnectionUsageInfo(a);
			previousUsage.RemoveRoom(roomId, type);
			currentUsage.RemoveRoom(roomId, type);

			IRouteSwitcherControl switcher = this.GetSourceControl(b) as IRouteSwitcherControl;
			if (switcher == null)
				return true;

			int output = b.Source.Address;

			switcher.ClearOutput(output, type);
			return true;
		}

		#endregion

		#region Controls

		/// <summary>
		/// Gets the devices for the given connection.
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public IEnumerable<IRouteControl> GetControls(Connection connection)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");

			yield return this.GetSourceControl(connection);
			yield return this.GetDestinationControl(connection);
		}

		/// <summary>
		/// Gets the control for the given device and control ids.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="control"></param>
		/// <returns></returns>
		private T GetControl<T>(int device, int control)
			where T : IRouteControl
		{
			return ServiceProvider.GetService<ICore>().GetControl<T>(device, control);
		}

		public IRouteDestinationControl GetDestinationControl(int device, int control)
		{
			return GetControl<IRouteDestinationControl>(device, control);
		}

		/// <summary>
		/// Gets the immediate destination device at the given address.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="address"></param>
		/// <param name="type"></param>
		/// <param name="destinationInput"></param>
		/// <returns></returns>
		public IRouteDestinationControl GetDestinationControl(IRouteSourceControl sourceControl, int address,
		                                                      eConnectionType type, out int destinationInput)
		{
			destinationInput = 0;

			if (sourceControl == null)
				throw new ArgumentNullException("sourceControl");

			Connection connection = Connections.GetOutputConnections(sourceControl.Parent.Id, sourceControl.Id, type)
			                                   .FirstOrDefault(c => c.Source.Address == address);
			if (connection == null)
				return null;

			destinationInput = connection.Destination.Address;
			return this.GetDestinationControl(connection);
		}

		/// <summary>
		/// Gets the source control with the given device and control ids.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="control"></param>
		/// <returns></returns>
		public IRouteSourceControl GetSourceControl(int device, int control)
		{
			return GetControl<IRouteSourceControl>(device, control);
		}

		/// <summary>
		/// Returns the immediate source devices from [1 -> input count] inclusive, including nulls.
		/// </summary>
		/// <param name="destination"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<IRouteSourceControl> GetSourceControls(IRouteDestinationControl destination, eConnectionType type)
		{
			if (destination == null)
				throw new ArgumentNullException("destination");

			int unused;
			return Connections.GetInputs(destination, type)
			                  .Select(i => GetSourceControl(destination, i, type, out unused));
		}

		/// <summary>
		/// Gets the immediate source device at the given address.
		/// </summary>
		/// <param name="destination"></param>
		/// <param name="address"></param>
		/// <param name="type"></param>
		/// <param name="sourceOutput"></param>
		/// <returns></returns>
		public IRouteSourceControl GetSourceControl(IRouteDestinationControl destination, int address, eConnectionType type,
		                                            out int sourceOutput)
		{
			sourceOutput = 0;

			if (destination == null)
				throw new ArgumentNullException("destination");

			Connection connection = Connections.GetInputConnections(destination.Parent.Id, destination.Id, type)
			                                   .FirstOrDefault(c => c.Destination.Address == address);
			if (connection == null)
				return null;

			sourceOutput = connection.Source.Address;
			return this.GetSourceControl(connection);
		}

		#endregion

		#region Switcher Callbacks

		/// <summary>
		/// Subscribes to the switchers found in the connections.
		/// </summary>
		private void SubscribeSwitchers()
		{
			UnsubscribeSwitchers();

			m_SubscribedSwitchers.AddRange(Connections.SelectMany(c => GetControls(c))
			                                          .OfType<IRouteSwitcherControl>()
			                                          .Distinct());

			foreach (IRouteSwitcherControl switcher in m_SubscribedSwitchers)
				Subscribe(switcher);
		}

		/// <summary>
		/// Unsubscribes from the previously subscribed switchers.
		/// </summary>
		private void UnsubscribeSwitchers()
		{
			foreach (IRouteSwitcherControl switcher in m_SubscribedSwitchers)
				Unsubscribe(switcher);
			m_SubscribedSwitchers.Clear();
		}

		/// <summary>
		/// Subscribe to the switcher events.
		/// </summary>
		/// <param name="switcher"></param>
		private void Subscribe(IRouteSwitcherControl switcher)
		{
			if (switcher == null)
				return;

			switcher.OnRouteChange += SwitcherOnRouteChange;
		}

		/// <summary>
		/// Unsubscribe from the switcher events.
		/// </summary>
		/// <param name="switcher"></param>
		private void Unsubscribe(IRouteSwitcherControl switcher)
		{
			if (switcher == null)
				return;

			switcher.OnRouteChange += SwitcherOnRouteChange;
		}

		/// <summary>
		/// Called when a switchers routing changes.
		/// We want to ensure that static routes remain in place after routing changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void SwitcherOnRouteChange(object sender, RouteChangeEventArgs args)
		{
			IRouteSwitcherControl switcher = sender as IRouteSwitcherControl;
			if (switcher == null)
				return;

			// Update connection ownership
			//ConnectionUsages.UpdateConnectionsUsage(switcher, args.Output, args.Type);

			// Re-enforce static routes
			StaticRoutes.ReApplyStaticRoutesForSwitcher(switcher);

			OnRouteChanged.Raise(this);
		}

		#endregion

		#region Settings

		protected override void ApplySettingsFinal(RoutingGraphSettings settings, IDeviceFactory factory)
		{
			m_Connections.OnConnectionsChanged -= ConnectionsOnConnectionsChanged;

			base.ApplySettingsFinal(settings, factory);

			IEnumerable<Connection> connections = GetConnections(settings, factory);
			IEnumerable<StaticRoute> staticRoutes = GetStaticRoutes(settings, factory);
			IEnumerable<ISource> sources = GetSources(settings, factory);
			IEnumerable<IDestination> destinations = GetDestinations(settings, factory);
			IEnumerable<IDestinationGroup> destinationGroups = GetDestinationGroups(settings, factory);

			Connections.SetConnections(connections);
			StaticRoutes.SetStaticRoutes(staticRoutes);
			Sources.SetChildren(sources);
			Destinations.SetChildren(destinations);
			DestinationGroups.SetChildren(destinationGroups);

			UnsubscribeSwitchers();
			m_SubscribedSwitchers.AddRange(Connections.GetConnections().SelectMany(c => GetFactorySwitcherControls(c, factory)));
			foreach (var switcher in m_SubscribedSwitchers)
				Subscribe(switcher);

			m_Connections.OnConnectionsChanged += ConnectionsOnConnectionsChanged;
		}

		private IEnumerable<StaticRoute> GetStaticRoutes(RoutingGraphSettings settings, IDeviceFactory factory)
		{
			return GetOriginatorsSkipExceptions<StaticRoute>(settings.StaticRouteSettings, factory);
		}

		private IEnumerable<ISource> GetSources(RoutingGraphSettings settings, IDeviceFactory factory)
		{
			return GetOriginatorsSkipExceptions<ISource>(settings.SourceSettings, factory);
		}

		private IEnumerable<IDestination> GetDestinations(RoutingGraphSettings settings, IDeviceFactory factory)
		{
			return GetOriginatorsSkipExceptions<IDestination>(settings.DestinationSettings, factory);
		}

		private IEnumerable<IDestinationGroup> GetDestinationGroups(RoutingGraphSettings settings, IDeviceFactory factory)
		{
			return GetOriginatorsSkipExceptions<IDestinationGroup>(settings.DestinationGroupSettings, factory);
		}

		private IEnumerable<Connection> GetConnections(RoutingGraphSettings settings, IDeviceFactory factory)
		{
			return GetOriginatorsSkipExceptions<Connection>(settings.ConnectionSettings, factory);
		}

		private IEnumerable<T> GetOriginatorsSkipExceptions<T>(IEnumerable<ISettings> originatorSettings, IDeviceFactory factory)
			where T : class, IOriginator
		{
			foreach (ISettings settings in originatorSettings)
			{
				T output;

				try
				{
					output = factory.GetOriginatorById<T>(settings.Id);
				}
				catch (Exception e)
				{
					Logger.AddEntry(eSeverity.Error, "Failed to instantiate {0} with id {1} - {2}", typeof(T).Name, settings.Id, e.Message);
					continue;
				}

				yield return output;
			}
		}

		private IEnumerable<IRouteSwitcherControl> GetFactorySwitcherControls(Connection c, IDeviceFactory factory)
		{
			var source = factory.GetOriginatorById<IDevice>(c.Source.Device);
			if (source.Controls.Contains(c.Source.Control) && source.Controls[c.Source.Control] is IRouteSwitcherControl)
				yield return source.Controls[c.Source.Control] as IRouteSwitcherControl;

			var destination = factory.GetOriginatorById<IDevice>(c.Destination.Device);
			if (destination.Controls.Contains(c.Destination.Control) && destination.Controls[c.Destination.Control] is IRouteSwitcherControl)
				yield return destination.Controls[c.Destination.Control] as IRouteSwitcherControl;
		}

		protected override void CopySettingsFinal(RoutingGraphSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.ConnectionSettings.SetRange(Connections.Where(c => c.Serialize).Select(r => r.CopySettings()).Cast<ISettings>());
			settings.StaticRouteSettings.SetRange(StaticRoutes.Where(c => c.Serialize).Select(r => r.CopySettings()).Cast<ISettings>());
			settings.SourceSettings.SetRange(Sources.Where(c => c.Serialize).Select(r => r.CopySettings()));
			settings.DestinationSettings.SetRange(Destinations.Where(c => c.Serialize).Select(r => r.CopySettings()));
			settings.DestinationGroupSettings.SetRange(DestinationGroups.Where(c => c.Serialize).Select(r => r.CopySettings()));
		}

		#endregion

		#region Console

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
			addRow("Connections Count", Connections.Count);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return
				new ConsoleCommand("PrintTable", "Prints a table of the routed devices and their input/output information.",
				                   () => PrintTable());
			yield return new ConsoleCommand("PrintConnections", "Prints the list of all connections.", () => PrintConnections());
			yield return new ConsoleCommand("PrintSources", "Prints the list of Sources", () => PrintSources());
			yield return new ConsoleCommand("PrintDestinations", "Prints the list of Destinations", () => PrintDestinations());
			yield return new ConsoleCommand("PrintUsages", "Prints a table of the connection usages.", () => PrintUsages());
		}

		private string PrintSources()
		{
			TableBuilder builder = new TableBuilder("Id", "Source");

			foreach (var source in m_Sources.GetChildren().OrderBy(c => c.Id))
				builder.AddRow(source.Id, source);

			return builder.ToString();
		}

		private string PrintDestinations()
		{
			TableBuilder builder = new TableBuilder("Id", "Destination");

			foreach (var destination in m_Destinations.GetChildren().OrderBy(c => c.Id))
				builder.AddRow(destination.Id, destination);

			return builder.ToString();
		}

		/// <summary>
		/// Loop over the devices, build a table of inputs, outputs, and their statuses.
		/// </summary>
		private string PrintTable()
		{
			RoutingGraphTableBuilder builder = new RoutingGraphTableBuilder(this);
			return builder.ToString();
		}

		private string PrintConnections()
		{
			TableBuilder builder = new TableBuilder("Source", "Output", "Destination", "Input", "Type");

			foreach (var con in m_Connections.GetConnections().OrderBy(c => c.Source.Device).ThenBy(c => c.Source.Address))
				builder.AddRow(con.Source, con.Source.Address, con.Destination, con.Destination.Address, con.ConnectionType);
			
			return builder.ToString();
		}

		/// <summary>
		/// Loop over the connections and build a table of usages.
		/// </summary>
		private string PrintUsages()
		{
			TableBuilder builder = new TableBuilder("Connection", "Type", "Source", "Rooms");

			Connection[] connections = Connections.ToArray();

			for (int index = 0; index < connections.Length; index++)
			{
				Connection connection = connections[index];
				ConnectionUsageInfo info = ConnectionUsages.GetConnectionUsageInfo(connection);
				int row = 0;

				foreach (eConnectionType type in EnumUtils.GetFlagsExceptNone(connection.ConnectionType))
				{
					string connectionString = row == 0 ? string.Format("{0} - {1}", connection.Id, connection.Name) : string.Empty;
					EndpointInfo? source = info.GetSource(type);
					int[] rooms = info.GetRooms(type).ToArray();
					string roomsString = rooms.Length == 0 ? string.Empty : StringUtils.ArrayFormat(rooms);

					builder.AddRow(connectionString, type, source, roomsString);

					row++;
				}

				if (index < connections.Length - 1)
					builder.AddSeparator();
			}

			return builder.ToString();
		}

		#endregion
	}
}
