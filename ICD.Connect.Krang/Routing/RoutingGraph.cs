using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Devices.Extensions;
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
    public sealed class RoutingGraph : AbstractRoutingGraph<RoutingGraphSettings>
    {
        private readonly IcdHashSet<IRouteSwitcherControl> m_SubscribedSwitchers;
        private readonly IcdHashSet<IRouteDestinationControl> m_SubscribedDestinations;
        private readonly IcdHashSet<IRouteSourceControl> m_SubscribedSources;

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
        public override event EventHandler<RouteFinishedEventArgs> OnRouteFinished;

        /// <summary>
        /// Raised when a switcher changes routing.
        /// </summary>
		public override event EventHandler OnRouteChanged;

        /// <summary>
        /// Raised when a source device starts/stops sending video.
        /// </summary>
		public override event EventHandler<EndpointStateEventArgs> OnSourceTransmissionStateChanged;

        /// <summary>
        /// Raised when a source device is connected or disconnected.
        /// </summary>
		public override event EventHandler<EndpointStateEventArgs> OnSourceDetectionStateChanged;

        #endregion

        #region Properties

        public override IConnectionsCollection Connections { get { return m_Connections; } }

		public override IOriginatorCollection<StaticRoute> StaticRoutes { get { return m_StaticRoutes; } }

		public override IConnectionUsageCollection ConnectionUsages { get { return m_ConnectionUsages; } }

		public override IOriginatorCollection<ISource> Sources { get { return m_Sources; } }

		public override IOriginatorCollection<IDestination> Destinations { get { return m_Destinations; } }

		public override IOriginatorCollection<IDestinationGroup> DestinationGroups { get { return m_DestinationGroups; } }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public RoutingGraph()
        {
            m_SubscribedSwitchers = new IcdHashSet<IRouteSwitcherControl>();
            m_SubscribedDestinations = new IcdHashSet<IRouteDestinationControl>();
            m_SubscribedSources = new IcdHashSet<IRouteSourceControl>();

            m_StaticRoutes = new StaticRoutesCollection(this);
            m_ConnectionUsages = new ConnectionUsageCollection(this);
            m_Connections = new ConnectionsCollection(this);
            m_Sources = new CoreSourceCollection();
            m_Destinations = new CoreDestinationCollection();
            m_DestinationGroups = new CoreDestinationGroupCollection();

            m_PendingRoutes = new Dictionary<Guid, int>();
            m_PendingRoutesSection = new SafeCriticalSection();
        }

        /// <summary>
        /// Override to release resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void DisposeFinal(bool disposing)
        {
            OnRouteFinished = null;
            OnRouteChanged = null;
            OnSourceTransmissionStateChanged = null;
            OnSourceDetectionStateChanged = null;

            base.DisposeFinal(disposing);
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
            SubscribeDestinations();
            SubscribeSources();

            m_StaticRoutes.UpdateStaticRoutes();
        }

        #endregion

        #region Recursion

        /// <summary>
        /// Finds the actively routed sources for the destination at the given input address.
        /// Will return multiple items when connection types are combined, e.g. seperate audio and video sources.
        /// </summary>
        /// <param name="destinationInput"></param>
        /// <param name="type"></param>
        /// <param name="signalDetected">When true skips inputs where no video is detected.</param>
        /// <param name="inputActive"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns>The sources</returns>
        public override IEnumerable<EndpointInfo> GetActiveSourceEndpoints(EndpointInfo destinationInput, eConnectionType type,
                                                                  bool signalDetected, bool inputActive)
        {
            IRouteDestinationControl destination = GetDestinationControl(destinationInput.Device, destinationInput.Control);
            if (destination == null)
                yield break;

            foreach (eConnectionType flag in EnumUtils.GetFlagsExceptNone(type))
            {
                EndpointInfo? endpoint = GetActiveSourceEndpoint(destination, destinationInput.Address, flag, signalDetected,
                                                                 inputActive);
                if (endpoint.HasValue)
                    yield return endpoint.Value;
            }
        }

        /// <summary>
        /// Finds the actively routed source for the destination at the given input address.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="input"></param>
        /// <param name="type"></param>
        /// <param name="signalDetected">When true skips inputs where no video is detected.</param>
        /// <param name="inputActive"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns>The source</returns>
		public override EndpointInfo? GetActiveSourceEndpoint(IRouteDestinationControl destination, int input,
                                                     eConnectionType type, bool signalDetected, bool inputActive)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");

            if (EnumUtils.HasMultipleFlags(type))
                throw new ArgumentNullException("type", "type must have a single flag");

            if (signalDetected && !destination.GetSignalDetectedState(input, type))
                return null;

            if (inputActive && !destination.GetInputActiveState(input, type))
                return null;

            Connection inputConnection = m_Connections.GetInputConnection(destination, input);
            if (inputConnection == null)
                return null;

            // Narrow the type by what the connection supports
            if (!inputConnection.ConnectionType.HasFlag(type))
                return null;

            IRouteSourceControl sourceControl = this.GetSourceControl(inputConnection);
            if (sourceControl == null)
                return null;

            IRouteMidpointControl sourceAsMidpoint = sourceControl as IRouteMidpointControl;
            if (sourceAsMidpoint == null)
                return sourceControl.GetOutputEndpointInfo(inputConnection.Source.Address);

            ConnectorInfo? sourceConnector = sourceAsMidpoint.GetInput(inputConnection.Source.Address, type);
            return sourceConnector.HasValue
                       ? GetActiveSourceEndpoint(sourceAsMidpoint, sourceConnector.Value.Address, type, signalDetected, inputActive)
                       : null;
        }

        /// <summary>
        /// Finds the destinations that the source is actively routed to.
        /// </summary>
        /// <param name="sourceControl"></param>
        /// <param name="sourceOutput"></param>
        /// <param name="type"></param>
        /// <param name="signalDetected">When true skips inputs where no video is detected.</param>
        /// <param name="inputActive"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns>The sources</returns>
		public override IEnumerable<EndpointInfo> GetActiveDestinationEndpoints(IRouteSourceControl sourceControl, int sourceOutput,
                                                                       eConnectionType type, bool signalDetected,
                                                                       bool inputActive)
        {
            if (sourceControl == null)
                throw new ArgumentNullException("sourceControl");

            return FindActivePaths(sourceControl.GetOutputEndpointInfo(sourceOutput), type, signalDetected, inputActive)
                .Select(p => p.Last().Destination);
        }

        /// <summary>
        /// Recurses over all of the source devices that can be routed to the destination.
        /// </summary>
        /// <param name="destinationControl"></param>
        /// <param name="type"></param>
        /// <returns></returns>
		public override IEnumerable<IRouteSourceControl> GetSourceControlsRecursive(IRouteDestinationControl destinationControl,
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
		public override bool SourceDetected(IRouteSourceControl sourceControl, eConnectionType type)
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
		public override bool SourceDetected(IRouteSourceControl sourceControl, int output, eConnectionType type)
        {
            if (sourceControl == null)
                throw new ArgumentNullException("sourceControl");

            int input;
            IRouteDestinationControl destination = GetDestinationControl(sourceControl, output, type, out input);
            return destination != null && destination.GetSignalDetectedState(input, type);
        }

        /// <summary>
        /// Finds the shortest available path from the source to the destination.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="flag"></param>
        /// <param name="roomId"></param>
		public override ConnectionPath FindPath(EndpointInfo source, EndpointInfo destination, eConnectionType flag, int roomId)
        {
            if (EnumUtils.HasMultipleFlags(flag))
				throw new ArgumentException("ConnectionType has multiple flags", "flag");

            // Ensure the source has a valid output connection
            Connection outputConnection = m_Connections.GetOutputConnection(source);
            if (outputConnection == null || !outputConnection.ConnectionType.HasFlag(flag))
                return null;

            // Ensure the destination has a valid input connection
            Connection inputConnection = m_Connections.GetInputConnection(destination);
            if (inputConnection == null || !inputConnection.ConnectionType.HasFlag(flag))
                return null;

            IEnumerable<Connection> path = RecursionUtils.BreadthFirstSearchPath(outputConnection, inputConnection,
                                                                                 c => GetConnectionChildren(source, c, flag, roomId));
            return path == null ? null : new ConnectionPath(path);
        }

		/// <summary>
		/// Returns the shortest paths from the source to the given destinations.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destinations"></param>
		/// <param name="flag"></param>
		/// <param name="roomId"></param>
		/// <returns></returns>
		public override IEnumerable<KeyValuePair<EndpointInfo, ConnectionPath>> FindPaths(
		    EndpointInfo source,
			IEnumerable<EndpointInfo> destinations,
		    eConnectionType flag,
		    int roomId)
	    {
		    if (destinations == null)
			    throw new ArgumentNullException("destinations");

		    if (EnumUtils.HasMultipleFlags(flag))
			    throw new ArgumentException("ConnectionType has multiple flags", "flag");

		    IcdHashSet<Connection> destionationConnections = new IcdHashSet<Connection>();
		    Dictionary<Connection, EndpointInfo> connectionToDestinations = new Dictionary<Connection, EndpointInfo>();

		    Connection sourceConnection = m_Connections.GetOutputConnection(source);

			foreach (EndpointInfo destination in destinations.Distinct())
		    {
			    // Ensure the source has a valid output connection.
			    if (sourceConnection == null || !sourceConnection.ConnectionType.HasFlag(flag))
			    {
					yield return new KeyValuePair<EndpointInfo, ConnectionPath>(destination, null);
				    continue;
			    }

			    // Ensure the destination has a valid input connection
			    Connection destinationConnection = m_Connections.GetInputConnection(destination);
			    if (destinationConnection == null || !destinationConnection.ConnectionType.HasFlag(flag))
			    {
					yield return new KeyValuePair<EndpointInfo, ConnectionPath>(destination, null);
				    continue;
			    }

			    destionationConnections.Add(destinationConnection);
			    connectionToDestinations.Add(destinationConnection, destination);
		    }

		    Dictionary<Connection, IEnumerable<Connection>> paths =
			    RecursionUtils.BreadthFirstSearchManyDestinations(sourceConnection,
			                                                      destionationConnections,
			                                                      c => GetConnectionChildren(source, c, flag, roomId));

		    foreach (KeyValuePair<Connection, IEnumerable<Connection>> kvp in paths)
		    {
			    ConnectionPath finalPath = kvp.Value == null ? null : new ConnectionPath(kvp.Value);
			    EndpointInfo destination = connectionToDestinations[kvp.Key];

				yield return new KeyValuePair<EndpointInfo, ConnectionPath>(destination, finalPath);
		    }
	    }

	    /// <summary>
        /// Gets the potential output connections for the given input connections.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="inputConnection"></param>
        /// <param name="type"></param>
        /// <param name="roomId"></param>
        /// <returns></returns>
        private IEnumerable<Connection> GetConnectionChildren(EndpointInfo source, Connection inputConnection, eConnectionType type, int roomId)
        {
            if (inputConnection == null)
                throw new ArgumentNullException("inputConnection");

            if (EnumUtils.HasMultipleFlags(type))
                throw new ArgumentException("ConnectionType has multiple flags", "type");

            return
                m_Connections.GetOutputConnections(inputConnection.Destination.Device,
                                                 inputConnection.Destination.Control,
                                                 type)
                           .Where(c =>
                                  ConnectionUsages.CanRouteConnection(c, source, roomId, type) &&
                                  c.IsAvailableToSourceDevice(source.Device) &&
                                  c.IsAvailableToRoom(roomId));
        }

        /// <summary>
        /// Finds the current paths from the given source to the destination.
        /// Return multiple paths if multiple connection types are provided.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="type"></param>
        /// <param name="signalDetected"></param>
        /// <param name="inputActive"></param>
        /// <returns></returns>
		public override IEnumerable<Connection[]> FindActivePaths(EndpointInfo source, EndpointInfo destination, eConnectionType type,
                                                         bool signalDetected, bool inputActive)
        {
            foreach (Connection[] path in FindActivePaths(source, type, signalDetected, inputActive))
            {
                // It's possible the path goes through our destination
                int index = path.FindIndex(c => c.Destination == destination);
                if (index < 0)
                    continue;

                yield return path.Take(index + 1).ToArray(index + 1);
            }
        }

        /// <summary>
        /// Finds all of the active paths from the given source.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="type"></param>
        /// <param name="signalDetected"></param>
        /// <param name="inputActive"></param>
        /// <returns></returns>
		public override IEnumerable<Connection[]> FindActivePaths(EndpointInfo source, eConnectionType type, bool signalDetected,
                                                         bool inputActive)
        {
            return EnumUtils.HasMultipleFlags(type)
                       ? EnumUtils.GetFlagsExceptNone(type).SelectMany(f => FindActivePaths(source, f, signalDetected, inputActive))
                       : FindActivePathsSingleFlag(source, type, signalDetected, inputActive);
        }

        /// <summary>
        /// Finds all of the active paths from the given source.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="type"></param>
        /// <param name="signalDetected"></param>
        /// <param name="inputActive"></param>
        /// <returns></returns>
        private IEnumerable<Connection[]> FindActivePathsSingleFlag(EndpointInfo source, eConnectionType type,
                                                                    bool signalDetected, bool inputActive)
        {
            IEnumerable<Connection[]> paths = FindActivePathsSingleFlag(source, type, signalDetected, inputActive, new List<Connection>());
            return paths;
        }

        /// <summary>
        /// Finds all of the active paths from the given source.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="type"></param>
        /// <param name="signalDetected"></param>
        /// <param name="inputActive"></param>
        /// <param name="visited"></param>
        /// <returns></returns>
        private IEnumerable<Connection[]> FindActivePathsSingleFlag(EndpointInfo source, eConnectionType type,
                                                                    bool signalDetected, bool inputActive, ICollection<Connection> visited)
        {
            if (!EnumUtils.HasSingleFlag(type))
                throw new ArgumentException("Type enum requires exactly 1 flag.", "type");

            // If there is no output connection from this source then we are done.
            Connection outputConnection = m_Connections.GetOutputConnection(source);
            if (outputConnection == null || !outputConnection.ConnectionType.HasFlag(type))
            {
                if (visited.Count > 0)
                    yield return visited.ToArray(visited.Count);
                yield break;
            }

            // If we care about signal detection state, don't follow this path if the source isn't detected by the destination.
            IRouteDestinationControl destination = this.GetDestinationControl(outputConnection);
            if (signalDetected)
            {
                if (destination == null || !destination.GetSignalDetectedState(outputConnection.Destination.Address, type))
                {
                    if (visited.Count > 0)
                        yield return visited.ToArray(visited.Count);
                    yield break;
                }
            }

            // If we care about input active state, don't follow this path if the input isn't active on the destination.
            if (inputActive)
            {
                if (destination == null || !destination.GetInputActiveState(outputConnection.Destination.Address, type))
                {
                    if (visited.Count > 0)
                        yield return visited.ToArray(visited.Count);
                    yield break;
                }
            }

            visited.Add(outputConnection);

            // Get the output addresses from the destination if it is a midpoint device.
            IRouteMidpointControl midpoint = destination as IRouteMidpointControl;
            if (midpoint == null)
            {
                if (visited.Count > 0)
                    yield return visited.ToArray(visited.Count);
                yield break;
            }

            int[] outputs = midpoint.GetOutputs(outputConnection.Destination.Address, type)
                                    .Select(c => c.Address)
                                    .ToArray();

            if (outputs.Length == 0)
            {
                if (visited.Count > 0)
                    yield return visited.ToArray(visited.Count);
                yield break;
            }

            // Recurse for each output.
            foreach (int outputAddress in outputs)
            {
                EndpointInfo newSource = midpoint.GetOutputEndpointInfo(outputAddress);

                IEnumerable<Connection[]> paths = FindActivePathsSingleFlag(newSource, type, signalDetected, inputActive,
                                                                            new List<Connection>(visited));
                foreach (Connection[] path in paths)
                    yield return path;
            }
        }

        /// <summary>
        /// Returns true if there is a path from the given source to the given destination.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="type"></param>
        /// <param name="roomId"></param>
        /// <returns></returns>
		public override bool HasPath(EndpointInfo source, EndpointInfo destination, eConnectionType type, int roomId)
        {
            return FindPath(source, destination, type, roomId) != null;
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
        public override void Route(IRouteSourceControl sourceControl, int sourceAddress, IRouteDestinationControl destinationControl,
                          int destinationAddress, eConnectionType type, int roomId)
        {
            if (sourceControl == null)
                throw new ArgumentNullException("sourceControl");

            if (destinationControl == null)
                throw new ArgumentNullException("destinationControl");

            EndpointInfo source = sourceControl.GetOutputEndpointInfo(sourceAddress);
            EndpointInfo destination = destinationControl.GetInputEndpointInfo(destinationAddress);

            Route(source, destination, type, roomId);
        }

        /// <summary>
        /// Routes the source to the destination.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="type"></param>
        /// <param name="roomId"></param>
		public override void Route(EndpointInfo source, EndpointInfo destination, eConnectionType type, int roomId)
        {
            RouteOperation operation = new RouteOperation
            {
                Source = source,
                Destination = destination,
                ConnectionType = type,
                RoomId = roomId
            };

            Route(operation);
        }

	    /// <summary>
	    /// Configures switchers to establish the given routing operation.
	    /// </summary>
	    /// <param name="op"></param>
		public override void Route(RouteOperation op)
	    {
		    if (op == null)
			    throw new ArgumentNullException("op");

		    foreach (eConnectionType type in EnumUtils.GetFlagsExceptNone(op.ConnectionType))
		    {
			    ConnectionPath path = FindPath(op.Source, op.Destination, type, op.RoomId);
			    RouteOperation operation = new RouteOperation(op) { ConnectionType = type };

			    if (path == null)
			    {
				    Logger.AddEntry(eSeverity.Error, "No path found for route {0}", operation);
				    continue;
			    }

			    RoutePath(operation, path);
		    }
	    }

	    /// <summary>
        /// Routes the source to the destinations.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destinations"></param>
        /// <param name="type"></param>
        /// <param name="roomId"></param>
		public override void RouteMultiple(EndpointInfo source, IEnumerable<EndpointInfo> destinations, eConnectionType type, int roomId)
        {
			if (destinations == null)
				throw new ArgumentNullException("destinations");

		    IList<EndpointInfo> destinationsList = destinations as IList<EndpointInfo> ?? destinations.ToList();

		    foreach (eConnectionType flag in EnumUtils.GetFlagsExceptNone(type))
		    {
				IEnumerable<KeyValuePair<EndpointInfo, ConnectionPath>> pathsForDestinations =
					FindPaths(source, destinationsList, flag, roomId);

			    foreach (KeyValuePair<EndpointInfo, ConnectionPath> kvp in pathsForDestinations)
			    {
					RouteOperation operation = new RouteOperation
					{
						Source = source,
						Destination = kvp.Key,
						ConnectionType = flag,
						RoomId = roomId
					};

					ConnectionPath path = kvp.Value;

					RoutePath(operation, path);
			    }
		    }
	    }

	    /// <summary>
        /// Applies the given path to the switchers.
        /// </summary>
        /// <param name="op"></param>
        /// <param name="path"></param>
		public override void RoutePath(RouteOperation op, IEnumerable<Connection> path)
        {
            if (op == null)
                throw new ArgumentNullException("op");

            if (path == null)
                throw new ArgumentNullException("path");

            int pendingRoutes;

            // Configure the switchers
            foreach (Connection[] pair in path.GetAdjacentPairs())
            {
                Connection connection = pair[0];
                Connection nextConnection = pair[1];

                RouteOperation switchOperation = new RouteOperation(op)
                {
                    LocalInput = connection.Destination.Address,
                    LocalOutput = nextConnection.Source.Address,
                };

                // Claim the connection leading up to the switcher
                //ConnectionUsages.ClaimConnection(connection, switchOperation);

                IRouteSwitcherControl switcher = this.GetDestinationControl(connection) as IRouteSwitcherControl;
                if (switcher == null)
                    continue;

                switcher.Route(switchOperation);
            }

            try
            {
                m_PendingRoutesSection.Enter();

                pendingRoutes = m_PendingRoutes.ContainsKey(op.Id) ? m_PendingRoutes[op.Id] : 0;
            }
            finally
            {
                m_PendingRoutesSection.Leave();
            }

            if (pendingRoutes > 0)
                return;

            OnRouteFinished.Raise(this, new RouteFinishedEventArgs(op, true));
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
        /// <param name="type"></param>
        /// <param name="roomId"></param>
        /// <returns></returns>
		public override void Unroute(IRouteSourceControl sourceControl, eConnectionType type, int roomId)
        {
            if (sourceControl == null)
                throw new ArgumentNullException("sourceControl");

            m_Connections.GetOutputsAny(sourceControl, type)
                       .ForEach(output => Unroute(sourceControl, output, type, roomId));
        }

        /// <summary>
        /// Searches for switchers currently routing the source and unroutes them.
        /// </summary>
        /// <param name="sourceControl"></param>
        /// <param name="sourceAddress"></param>
        /// <param name="type"></param>
        /// <param name="roomId"></param>
		public override void Unroute(IRouteSourceControl sourceControl, int sourceAddress, eConnectionType type, int roomId)
        {
            if (sourceControl == null)
                throw new ArgumentNullException("sourceControl");

            Unroute(sourceControl.GetOutputEndpointInfo(sourceAddress), type, roomId);
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
		public override void Unroute(IRouteSourceControl sourceControl, int sourceAddress, IRouteDestinationControl destinationControl,
                            int destinationAddress, eConnectionType type, int roomId)
        {
            if (sourceControl == null)
                throw new ArgumentNullException("sourceControl");

            if (destinationControl == null)
                throw new ArgumentNullException("destinationControl");

            Unroute(sourceControl.GetOutputEndpointInfo(sourceAddress),
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
		public override void Unroute(IRouteSourceControl sourceControl, int sourceAddress, IRouteDestinationControl destinationControl,
                            eConnectionType type, int roomId)
        {
            if (sourceControl == null)
                throw new ArgumentNullException("sourceControl");

            if (destinationControl == null)
                throw new ArgumentNullException("destinationControl");

            m_Connections.GetInputsAny(destinationControl, type)
                       .ForEach(input => Unroute(sourceControl, sourceAddress, destinationControl, input, type, roomId));
        }

        /// <summary>
        /// Unroutes every path from the given source to the destination.
        /// </summary>
        /// <param name="sourceControl"></param>
        /// <param name="destinationControl"></param>
        /// <param name="type"></param>
        /// <param name="roomId"></param>
        /// <returns>False if the devices could not be unrouted.</returns>
		public override void Unroute(IRouteSourceControl sourceControl, IRouteDestinationControl destinationControl,
                            eConnectionType type, int roomId)
        {
            if (sourceControl == null)
                throw new ArgumentNullException("sourceControl");

            if (destinationControl == null)
                throw new ArgumentNullException("destinationControl");

            m_Connections.GetOutputsAny(sourceControl, type)
                       .ForEach(output => Unroute(sourceControl, output, destinationControl, type, roomId));
        }

        /// <summary>
        /// Searches for switchers currently routing the source and unroutes them.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="type"></param>
        /// <param name="roomId"></param>
        private void Unroute(EndpointInfo source, eConnectionType type, int roomId)
        {
            foreach (Connection[] path in FindActivePaths(source, type, false, false))
            {
                // Loop backwards looking for switchers closest to the destination
                for (int index = path.Length - 1; index > 0; index--)
                {
                    Connection previous = path[index - 1];
                    Connection current = path[index];

                    if (!Unroute(previous, current, type, roomId))
                        break;
                }
            }
        }

        /// <summary>
        /// Searches for switchers currently routing the source to the destination and unroutes them.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="type"></param>
        /// <param name="roomId"></param>
        /// <returns>False if the devices could not be unrouted.</returns>
		public override void Unroute(EndpointInfo source, EndpointInfo destination, eConnectionType type, int roomId)
        {
            foreach (Connection[] path in FindActivePaths(source, destination, type, false, false))
                Unroute(path, type, roomId);
        }

        /// <summary>
        /// Unroutes the given connection path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <param name="roomId"></param>
		public override void Unroute(Connection[] path, eConnectionType type, int roomId)
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

                // Stop unrouting if the input is routed to other outputs - we reached a fork
                int input = previous.Destination.Address;
                if (midpoint.GetOutputs(input, type).Any())
                    break;
            }
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
		public override IEnumerable<IRouteControl> GetControls(Connection connection)
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

		public override IRouteDestinationControl GetDestinationControl(int device, int control)
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
		public override IRouteDestinationControl GetDestinationControl(IRouteSourceControl sourceControl, int address,
                                                              eConnectionType type, out int destinationInput)
        {
            destinationInput = 0;

            if (sourceControl == null)
                throw new ArgumentNullException("sourceControl");

            Connection connection = m_Connections.GetOutputConnections(sourceControl.Parent.Id, sourceControl.Id, type)
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
		public override IRouteSourceControl GetSourceControl(int device, int control)
        {
            return GetControl<IRouteSourceControl>(device, control);
        }

        /// <summary>
        /// Returns the immediate source devices from [1 -> input count] inclusive, including nulls.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="type"></param>
        /// <returns></returns>
		public override IEnumerable<IRouteSourceControl> GetSourceControls(IRouteDestinationControl destination, eConnectionType type)
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
		public override IRouteSourceControl GetSourceControl(IRouteDestinationControl destination, int address, eConnectionType type,
                                                    out int sourceOutput)
        {
            sourceOutput = 0;

            if (destination == null)
                throw new ArgumentNullException("destination");

            Connection connection = m_Connections.GetInputConnections(destination.Parent.Id, destination.Id, type)
                                               .FirstOrDefault(c => c.Destination.Address == address);
            if (connection == null)
                return null;

            sourceOutput = connection.Source.Address;
            return this.GetSourceControl(connection);
        }

        #endregion

        #region Destination Callbacks

        /// <summary>
        /// Unsubscribe from the previous destination controls and subscribe to the new destination control events.
        /// </summary>
        private void SubscribeDestinations()
        {
            UnsubscribeDestinations();

            m_SubscribedDestinations.AddRange(Connections.SelectMany(c => GetControls(c))
                                                         .OfType<IRouteDestinationControl>()
                                                         .Distinct());

            foreach (IRouteDestinationControl destination in m_SubscribedDestinations)
                Subscribe(destination);
        }

        /// <summary>
        /// Unsubscribe from the previous destination control events.
        /// </summary>
        private void UnsubscribeDestinations()
        {
            foreach (IRouteDestinationControl destinationControl in m_SubscribedDestinations)
                Unsubscribe(destinationControl);
            m_SubscribedDestinations.Clear();
        }

        /// <summary>
        /// Subscribe to the destination control events.
        /// </summary>
        /// <param name="destinationControl"></param>
        private void Subscribe(IRouteDestinationControl destinationControl)
        {
            if (destinationControl == null)
                return;

            destinationControl.OnSourceDetectionStateChange += DestinationControlOnSourceDetectionStateChange;
        }

        /// <summary>
        /// Unsubscribe from the destination control events.
        /// </summary>
        /// <param name="destinationControl"></param>
        private void Unsubscribe(IRouteDestinationControl destinationControl)
        {
            if (destinationControl == null)
                return;

            destinationControl.OnSourceDetectionStateChange -= DestinationControlOnSourceDetectionStateChange;
        }

        /// <summary>
        /// Called when a destination control source detection state changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void DestinationControlOnSourceDetectionStateChange(object sender, SourceDetectionStateChangeEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            IRouteDestinationControl destination = sender as IRouteDestinationControl;
            if (destination == null)
                return;

            int output;
            IRouteSourceControl source = GetSourceControl(destination, args.Input, args.Type, out output);
            if (source == null)
                return;

            EndpointInfo info = source.GetOutputEndpointInfo(output);
            OnSourceDetectionStateChanged.Raise(this, new EndpointStateEventArgs(info, args.State));
        }

        #endregion

        #region Source Callbacks

        /// <summary>
        /// Unsubscribe from the previous source controls and subscribe to the current source events.
        /// </summary>
        private void SubscribeSources()
        {
            UnsubscribeSources();

            m_SubscribedSources.AddRange(Connections.SelectMany(c => GetControls(c))
                                                    .OfType<IRouteSourceControl>()
                                                    .Distinct());

            foreach (IRouteSourceControl source in m_SubscribedSources)
                Subscribe(source);
        }

        /// <summary>
        /// Unsubscribe from the previous source control events.
        /// </summary>
        private void UnsubscribeSources()
        {
            foreach (IRouteSourceControl control in m_SubscribedSources)
                Unsubscribe(control);
            m_SubscribedSources.Clear();
        }

        /// <summary>
        /// Subscribe to the source control events.
        /// </summary>
        /// <param name="sourceControl"></param>
        private void Subscribe(IRouteSourceControl sourceControl)
        {
            if (sourceControl == null)
                return;

            sourceControl.OnActiveTransmissionStateChanged += SourceControlOnActiveTransmissionStateChanged;
        }

        /// <summary>
        /// Unsubscribe from the source control events.
        /// </summary>
        /// <param name="sourceControl"></param>
        private void Unsubscribe(IRouteSourceControl sourceControl)
        {
            if (sourceControl == null)
                return;

            sourceControl.OnActiveTransmissionStateChanged -= SourceControlOnActiveTransmissionStateChanged;
        }

        /// <summary>
        /// Called when a source control starts/stops sending video.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void SourceControlOnActiveTransmissionStateChanged(object sender, TransmissionStateEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            IRouteSourceControl source = sender as IRouteSourceControl;
            if (source == null)
                return;

            EndpointInfo endpoint = source.GetOutputEndpointInfo(args.Output);
            OnSourceTransmissionStateChanged.Raise(this, new EndpointStateEventArgs(endpoint, args.State));
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

            switcher.OnRouteChange -= SwitcherOnRouteChange;
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
            m_StaticRoutes.ReApplyStaticRoutesForSwitcher(switcher);

            OnRouteChanged.Raise(this);
        }

        #endregion

        #region Settings

        protected override void ApplySettingsFinal(RoutingGraphSettings settings, IDeviceFactory factory)
        {
            m_Connections.OnChildrenChanged -= ConnectionsOnConnectionsChanged;

            base.ApplySettingsFinal(settings, factory);

            IEnumerable<Connection> connections = GetConnections(settings, factory);
            IEnumerable<StaticRoute> staticRoutes = GetStaticRoutes(settings, factory);
            IEnumerable<ISource> sources = GetSources(settings, factory);
            IEnumerable<IDestination> destinations = GetDestinations(settings, factory);
            IEnumerable<IDestinationGroup> destinationGroups = GetDestinationGroups(settings, factory);

            m_Connections.SetChildren(connections);
			m_StaticRoutes.SetChildren(staticRoutes);
            m_Sources.SetChildren(sources);
            m_Destinations.SetChildren(destinations);
            m_DestinationGroups.SetChildren(destinationGroups);

            SubscribeSwitchers();
            SubscribeDestinations();
            SubscribeSources();

            m_Connections.OnChildrenChanged += ConnectionsOnConnectionsChanged;
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
                    Logger.AddEntry(eSeverity.Error, "{0} failed to instantiate {1} with id {2} - {3}", this, typeof(T).Name,
                                    settings.Id, e.Message);
                    continue;
                }

                yield return output;
            }
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
    }
}
