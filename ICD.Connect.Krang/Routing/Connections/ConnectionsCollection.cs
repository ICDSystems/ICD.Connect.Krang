using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.Endpoints;

namespace ICD.Connect.Krang.Routing.Connections
{
	public sealed class ConnectionsCollection : IConnectionsCollection
	{
		public event EventHandler OnConnectionsChanged;

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
		/// Gets the number of connections.
		/// </summary>
		public int Count { get { return m_ConnectionsSection.Execute(() => m_Connections.Count); } }

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
		/// <param name="destinationControl"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		[CanBeNull]
		public Connection GetInputConnection(IRouteDestinationControl destinationControl, int input)
		{
			if (destinationControl == null)
				throw new ArgumentNullException("destinationControl");

			DeviceControlInfo key = new DeviceControlInfo(destinationControl.Parent.Id, destinationControl.Id);

			m_ConnectionsSection.Enter();

			try
			{
				Dictionary<int, Connection> map;
				return m_InputConnectionLookup.TryGetValue(key, out map)
					       ? map.GetDefault(input, null)
					       : null;
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
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
						   ? map.Values.Where(c => EnumUtils.HasFlags(c.ConnectionType, type)).ToArray()
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
						   ? map.Values.Where(c => EnumUtils.HasAnyFlags(c.ConnectionType, type)).ToArray()
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
					       ? map.Values.ToArray()
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
						   ? map.Values.Where(c => EnumUtils.HasFlags(c.ConnectionType, type)).ToArray()
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
						   ? map.Values.Where(c => EnumUtils.HasAnyFlags(c.ConnectionType, type)).ToArray()
						   : Enumerable.Empty<Connection>();
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the connection with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Connection GetConnection(int id)
		{
			return m_ConnectionsSection.Execute(() => m_Connections[id]);
		}

		/// <summary>
		/// Gets all of the connections.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Connection> GetConnections()
		{
			return m_ConnectionsSection.Execute(() => m_Connections.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Clears and sets the connections.
		/// </summary>
		/// <param name="connections"></param>
		public void SetConnections(IEnumerable<Connection> connections)
		{
			if (connections == null)
				throw new ArgumentNullException("connections");

			m_ConnectionsSection.Enter();

			try
			{
				m_Connections.Clear();
				m_Connections.AddRange(connections, c => c.Id);

				UpdateLookups();
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}

			OnConnectionsChanged.Raise(this);
		}

		/// <summary>
		/// Clears the connections.
		/// </summary>
		public void Clear()
		{
			SetConnections(Enumerable.Empty<Connection>());
		}

		/// <summary>
		/// Returns true if the collection contains a connection with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool ContainsConnection(int id)
		{
			return m_ConnectionsSection.Execute(() => m_Connections.ContainsKey(id));
		}

		/// <summary>
		/// Returns true if the collection contains the given connection.
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public bool ContainsConnection(Connection connection)
		{
			return connection != null &&
			       ContainsConnection(connection.Id) &&
			       GetConnection(connection.Id) == connection;
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
					.Order()
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

			m_ConnectionsSection.Enter();

			try
			{
				return GetInputConnections(destinationControl.Parent.Id, destinationControl.Id, type)
					.Select(c => c.Destination.Address)
					.Order()
					.ToArray();
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
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
			m_ConnectionsSection.Enter();

			try
			{
				return GetInputConnectionsAny(destinationDeviceId, destinationControlId, type)
					.Select(c => c.Destination.Address)
					.Order()
					.ToArray();
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
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
			m_ConnectionsSection.Enter();

			try
			{
				return GetOutputConnections(sourceDeviceId, sourceControlId, type)
					.Select(c => c.Source.Address)
					.Order()
					.ToArray();
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
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

			m_ConnectionsSection.Enter();

			try
			{
				return GetOutputConnections(sourceControl.Parent.Id, sourceControl.Id, type)
					.Where(c => c.Destination.Device == destinationControl.Parent.Id &&
					            c.Destination.Control == destinationControl.Id)
					.Select(c => c.Source.Address)
					.Order()
					.ToArray();
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
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
			m_ConnectionsSection.Enter();

			try
			{
				return GetOutputConnectionsAny(sourceDeviceId, sourceControlId, type)
					.Select(c => c.Source.Address)
					.Order()
					.ToArray();
			}
			finally
			{
				m_ConnectionsSection.Leave();
			}
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

		public IEnumerator<Connection> GetEnumerator()
		{
			return GetConnections().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
