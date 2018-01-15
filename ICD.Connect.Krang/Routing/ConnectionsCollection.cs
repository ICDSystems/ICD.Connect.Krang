using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.Endpoints;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.Routing
{
	public sealed class ConnectionsCollection : AbstractOriginatorCollection<Connection>, IConnectionsCollection
	{
		private readonly Dictionary<int, Connection> m_Connections;
		private readonly SafeCriticalSection m_ConnectionsSection;

		/// <summary>
		/// Maps Device -> Control -> Address -> outgoing connections.
		/// </summary>
		private readonly Dictionary<DeviceControlInfo, Dictionary<int, Connection>> m_OutputConnectionLookup;

		/// <summary>
		/// Maps Device -> Control -> Address -> incoming connections.
		/// </summary>
		private readonly Dictionary<DeviceControlInfo, Dictionary<int, Connection>> m_InputConnectionLookup;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="routingGraph"></param>
		public ConnectionsCollection(RoutingGraph routingGraph)
		{
			m_Connections = new Dictionary<int, Connection>();
			m_OutputConnectionLookup = new Dictionary<DeviceControlInfo, Dictionary<int, Connection>>();
			m_InputConnectionLookup = new Dictionary<DeviceControlInfo, Dictionary<int, Connection>>();
			m_ConnectionsSection = new SafeCriticalSection();

			UpdateLookups();
		}

		#region Methods

		/// <summary>
		/// Gets the connection for the given endpoint.
		/// </summary>
		/// <param name="destination"></param>
		/// <returns></returns>
		[CanBeNull]
		public Connection GetInputConnection(EndpointInfo destination)
		{
			DeviceControlInfo key = new DeviceControlInfo(destination.Device, destination.Control);

			m_ConnectionsSection.Enter();

			try
			{
				Dictionary<int, Connection> map;
				return m_InputConnectionLookup.TryGetValue(key, out map)
					       ? map.GetDefault(destination.Address, null)
					       : null;
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the connection for the given endpoint.
		/// </summary>
		/// <param name="destinationControl"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		[CanBeNull]
		public Connection GetInputConnection(IRouteDestinationControl destinationControl, int input)
		{
			if (destinationControl == null)
				throw new ArgumentNullException("destinationControl");

			return GetInputConnection(destinationControl.GetInputEndpointInfo(input));
		}

		/// <summary>
		/// Gets the input connections for the device with the given type.
		/// </summary>
		/// <param name="destinationDeviceId"></param>
		/// <param name="destinationControlId"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<Connection> GetInputConnections(int destinationDeviceId, int destinationControlId,
		                                                   eConnectionType type)
		{
			DeviceControlInfo info = new DeviceControlInfo(destinationDeviceId, destinationControlId);

			m_ConnectionsSection.Enter();

			try
			{
				Dictionary<int, Connection> map;
				return m_InputConnectionLookup.TryGetValue(info, out map)
					       ? map.Values
					            .Where(c => EnumUtils.HasFlags(c.ConnectionType, type))
					            .ToArray()
					       : Enumerable.Empty<Connection>();
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the input connections for the device matching any of the given type flags.
		/// </summary>
		/// <param name="destinationDeviceId"></param>
		/// <param name="destinationControlId"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<Connection> GetInputConnectionsAny(int destinationDeviceId, int destinationControlId,
		                                                      eConnectionType type)
		{
			DeviceControlInfo info = new DeviceControlInfo(destinationDeviceId, destinationControlId);

			m_ConnectionsSection.Enter();

			try
			{
				Dictionary<int, Connection> map;
				return m_InputConnectionLookup.TryGetValue(info, out map)
					       ? map.Values
					            .Where(c => EnumUtils.HasAnyFlags(c.ConnectionType, type))
					            .ToArray()
					       : Enumerable.Empty<Connection>();
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the connection for the given endpoint.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		[CanBeNull]
		public Connection GetOutputConnection(EndpointInfo source)
		{
			DeviceControlInfo key = new DeviceControlInfo(source.Device, source.Control);

			m_ConnectionsSection.Enter();

			try
			{
				Dictionary<int, Connection> map;
				return m_OutputConnectionLookup.TryGetValue(key, out map)
					       ? map.GetDefault(source.Address, null)
					       : null;
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the connection for the given endpoint.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		[CanBeNull]
		public Connection GetOutputConnection(IRouteSourceControl sourceControl, int output)
		{
			if (sourceControl == null)
				throw new ArgumentNullException("sourceControl");

			return GetOutputConnection(sourceControl.GetOutputEndpointInfo(output));
		}

		/// <summary>
		/// Gets the output connections for the given source device.
		/// </summary>
		/// <param name="sourceDeviceId"></param>
		/// <param name="sourceControlId"></param>
		/// <returns></returns>
		public IEnumerable<Connection> GetOutputConnections(int sourceDeviceId, int sourceControlId)
		{
			DeviceControlInfo info = new DeviceControlInfo(sourceDeviceId, sourceControlId);

			m_ConnectionsSection.Enter();

			try
			{
				Dictionary<int, Connection> map;
				return m_OutputConnectionLookup.TryGetValue(info, out map)
					       ? map.Values.ToArray(map.Count)
					       : Enumerable.Empty<Connection>();
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the output connections for the given source device.
		/// </summary>
		/// <param name="sourceDeviceId"></param>
		/// <param name="sourceControlId"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<Connection> GetOutputConnections(int sourceDeviceId, int sourceControlId, eConnectionType type)
		{
			DeviceControlInfo info = new DeviceControlInfo(sourceDeviceId, sourceControlId);

			m_ConnectionsSection.Enter();

			try
			{
				Dictionary<int, Connection> map;
				return m_OutputConnectionLookup.TryGetValue(info, out map)
					       ? map.Values
					            .Where(c => EnumUtils.HasFlags(c.ConnectionType, type))
					            .ToArray()
					       : Enumerable.Empty<Connection>();
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the output connections for the given source device matching any of the given type flags.
		/// </summary>
		/// <param name="sourceDeviceId"></param>
		/// <param name="sourceControlId"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<Connection> GetOutputConnectionsAny(int sourceDeviceId, int sourceControlId, eConnectionType type)
		{
			DeviceControlInfo info = new DeviceControlInfo(sourceDeviceId, sourceControlId);

			m_ConnectionsSection.Enter();

			try
			{
				Dictionary<int, Connection> map;
				return m_OutputConnectionLookup.TryGetValue(info, out map)
					       ? map.Values
					            .Where(c => EnumUtils.HasAnyFlags(c.ConnectionType, type))
					            .ToArray()
					       : Enumerable.Empty<Connection>();
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
		}

		protected override void ChildAdded(Connection child)
		{
			base.ChildAdded(child);

			UpdateLookups();
		}

		#endregion

		#region Adjacency

		/// <summary>
		/// Returns the destination input addresses where source and destination are directly connected.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="destinationControl"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<int> GetInputs(IRouteSourceControl sourceControl, IRouteDestinationControl destinationControl,
		                                  eConnectionType type)
		{
			if (sourceControl == null)
				throw new ArgumentNullException("sourceControl");

			if (destinationControl == null)
				throw new ArgumentNullException("destinationControl");

			m_ConnectionsSection.Enter();

			try
			{
				return GetInputConnections(destinationControl.Parent.Id, destinationControl.Id, type)
					.Where(c => c.Source.Device == sourceControl.Parent.Id && c.Source.Control == sourceControl.Id)
					.Select(c => c.Destination.Address)
					.ToArray();
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
		}

		/// <summary>
		/// Returns the destination input addresses.
		/// </summary>
		/// <param name="destinationControl"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<int> GetInputs(IRouteDestinationControl destinationControl, eConnectionType type)
		{
			if (destinationControl == null)
				throw new ArgumentNullException("destinationControl");

			return GetInputConnections(destinationControl.Parent.Id, destinationControl.Id, type)
				.Select(c => c.Destination.Address);
		}

		/// <summary>
		/// Gets the outputs that match any of the given type flags.
		/// </summary>
		/// <param name="destinationControl"></param>
		/// <param name="type"></param>
		public IEnumerable<int> GetInputsAny(IRouteDestinationControl destinationControl, eConnectionType type)
		{
			if (destinationControl == null)
				throw new ArgumentNullException("destinationControl");

			return GetInputsAny(destinationControl.Parent.Id, destinationControl.Id, type);
		}

		/// <summary>
		/// Gets the mapped input addresses for the given destination control matching any of the given type flags.
		/// </summary>
		/// <param name="destinationDeviceId"></param>
		/// <param name="destinationControlId"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<int> GetInputsAny(int destinationDeviceId, int destinationControlId, eConnectionType type)
		{
			return GetInputConnectionsAny(destinationDeviceId, destinationControlId, type)
				.Select(c => c.Destination.Address);
		}

		/// <summary>
		/// Gets the mapped output addresses for the given source control.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<int> GetOutputs(IRouteSourceControl sourceControl, eConnectionType type)
		{
			if (sourceControl == null)
				throw new ArgumentNullException("sourceControl");

			return GetOutputs(sourceControl.Parent.Id, sourceControl.Id, type);
		}

		/// <summary>
		/// Gets the mapped output addresses for the given source control.
		/// </summary>
		/// <param name="sourceDeviceId"></param>
		/// <param name="sourceControlId"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<int> GetOutputs(int sourceDeviceId, int sourceControlId, eConnectionType type)
		{
			return GetOutputConnections(sourceDeviceId, sourceControlId, type)
				.Select(c => c.Source.Address);
		}

		/// <summary>
		/// Returns the source output addresses where source and destination are directly connected.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="destinationControl"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<int> GetOutputs(IRouteSourceControl sourceControl, IRouteDestinationControl destinationControl,
		                                   eConnectionType type)
		{
			if (sourceControl == null)
				throw new ArgumentNullException("sourceControl");

			if (destinationControl == null)
				throw new ArgumentNullException("destinationControl");

			return GetOutputConnections(sourceControl.Parent.Id, sourceControl.Id, type)
				.Where(c => c.Destination.Device == destinationControl.Parent.Id &&
				            c.Destination.Control == destinationControl.Id)
				.Select(c => c.Source.Address);
		}

		/// <summary>
		/// Gets the outputs that match any of the given type flags.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<int> GetOutputsAny(IRouteSourceControl sourceControl, eConnectionType type)
		{
			if (sourceControl == null)
				throw new ArgumentNullException("sourceControl");

			return GetOutputsAny(sourceControl.Parent.Id, sourceControl.Id, type);
		}

		/// <summary>
		/// Gets the mapped output addresses for the given source control matching any of the given type flags.
		/// </summary>
		/// <param name="sourceDeviceId"></param>
		/// <param name="sourceControlId"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<int> GetOutputsAny(int sourceDeviceId, int sourceControlId, eConnectionType type)
		{
			return GetOutputConnectionsAny(sourceDeviceId, sourceControlId, type)
				.Select(c => c.Source.Address);
		}

		#endregion

		private void UpdateLookups()
		{
			m_ConnectionsSection.Enter();

			try
			{
				m_OutputConnectionLookup.Clear();
				m_InputConnectionLookup.Clear();

				foreach (Connection connection in m_Connections.Values)
				{
					DeviceControlInfo sourceInfo = new DeviceControlInfo(connection.Source.Device, connection.Source.Control);
					DeviceControlInfo destinationInfo = new DeviceControlInfo(connection.Destination.Device,
					                                                          connection.Destination.Control);

					// Add device controls to the maps
					if (!m_OutputConnectionLookup.ContainsKey(sourceInfo))
						m_OutputConnectionLookup.Add(sourceInfo, new Dictionary<int, Connection>());
					if (!m_InputConnectionLookup.ContainsKey(destinationInfo))
						m_InputConnectionLookup.Add(destinationInfo,new Dictionary<int, Connection>());

					// Add connections to the maps
					m_OutputConnectionLookup[sourceInfo][connection.Source.Address] = connection;
					m_InputConnectionLookup[destinationInfo][connection.Destination.Address] = connection;
				}
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
		}
	}
}
