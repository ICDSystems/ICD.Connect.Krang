using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.ConnectionUsage;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.Endpoints;

namespace ICD.Connect.Krang.Routing
{
	public sealed class ConnectionUsageCollection : IConnectionUsageCollection
	{
		/// <summary>
		/// Callback is called for each ConnectionUsageInfo recursively.
		/// If the callback returns false recursion stops for the current branch.
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		private delegate bool RecurseConnectionInfoCallback(ConnectionUsageInfo info);

		private readonly RoutingGraph m_RoutingGraph;

		private readonly Dictionary<Connection, ConnectionUsageInfo> m_ConnectionsUsage;
		private readonly SafeCriticalSection m_ConnectionsUsageSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="routingGraph"></param>
		public ConnectionUsageCollection(RoutingGraph routingGraph)
		{
			m_ConnectionsUsage = new Dictionary<Connection, ConnectionUsageInfo>();
			m_ConnectionsUsageSection = new SafeCriticalSection();

			m_RoutingGraph = routingGraph;
		}

		#region Methods

		/// <summary>
		/// Clears the cache of how the connections are being used. This can free up previously used connections for new routes.
		/// </summary>
		[PublicAPI]
		public void Clear()
		{
			m_ConnectionsUsageSection.Execute(() => m_ConnectionsUsage.Clear());
		}

		/// <summary>
		/// Gets the usage info for the given connection.
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		[PublicAPI]
		public ConnectionUsageInfo GetConnectionUsageInfo(Connection connection)
		{
			return LazyLoadConnectionUsageInfo(connection);
		}

		/// <summary>
		/// Removes usages where connections no longer exist.
		/// </summary>
		public void RemoveInvalid()
		{
			m_ConnectionsUsageSection.Enter();

			try
			{
				foreach (
					KeyValuePair<Connection, ConnectionUsageInfo> item in
						m_ConnectionsUsage.Where(kvp => !m_RoutingGraph.Connections.Contains(kvp.Key)).ToArray())
					m_ConnectionsUsage.Remove(item.Key);
			}
			finally
			{
				m_ConnectionsUsageSection.Leave();
			}
		}

		/// <summary>
		/// Updates connection usages to match the new state of the switcher.
		/// </summary>
		/// <param name="switcher"></param>
		/// <param name="output"></param>
		/// <param name="type"></param>
		public void UpdateConnectionsUsage(IRouteSwitcherControl switcher, int output, eConnectionType type)
		{
			foreach (eConnectionType flag in EnumUtils.GetFlagsExceptNone(type))
			{
				ConnectorInfo? connector = switcher.GetInput(output, flag);

				// If the input is null the switcher has unrouted the output, so we need to recurse forwards and clear usages
				if (connector == null)
				{
					ReleaseConnection(switcher, output, flag);
					continue;
				}

				int input = ((ConnectorInfo)connector).Address;

				// If the input has changed for the output we need to recurse forwards and update usages
				Connection inputConnection = m_RoutingGraph.Connections.GetInputConnection(switcher, input);

				// If there's no connection we have no idea what the source is
				if (inputConnection == null || !inputConnection.ConnectionType.HasFlag(flag))
				{
					PropogateConnectionSource(switcher, output, flag, null);
					continue;
				}

				ConnectionUsageInfo inputUsage = LazyLoadConnectionUsageInfo(inputConnection);
				EndpointInfo? source = inputUsage.GetSource(flag);

				PropogateConnectionSource(switcher, output, flag, source);
			}
		}

		/// <summary>
		/// Returns true if the current usage of the connection will allow the given source for the given room.
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="source"></param>
		/// <param name="roomId"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public bool CanRouteConnection(Connection connection, EndpointInfo source, int roomId, eConnectionType type)
		{
			ConnectionUsageInfo info = LazyLoadConnectionUsageInfo(connection);
			return info.CanRoute(source, roomId, type);
		}

		/// <summary>
		/// Called before the RoutingGraph routes a switcher. Flags the connection as being in use.
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="operation"></param>
		public void ClaimConnection(Connection connection, RouteOperation operation)
		{
			ConnectionUsageInfo info = LazyLoadConnectionUsageInfo(connection);
			info.Claim(operation.Source, operation.RoomId, operation.ConnectionType);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets or instantiates the ConnectionUsageInfo for the given Connection.
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		private ConnectionUsageInfo LazyLoadConnectionUsageInfo(Connection connection)
		{
			m_ConnectionsUsageSection.Enter();

			try
			{
				if (!m_ConnectionsUsage.ContainsKey(connection))
					m_ConnectionsUsage[connection] = new ConnectionUsageInfo();
				return m_ConnectionsUsage[connection];
			}
			finally
			{
				m_ConnectionsUsageSection.Leave();
			}
		}

		/// <summary>
		/// Recurses forwards from the device, releasing connection usages.
		/// </summary>
		/// <param name="sourceDevice"></param>
		/// <param name="output"></param>
		/// <param name="type"></param>
		private void ReleaseConnection(IRouteSourceControl sourceDevice, int output, eConnectionType type)
		{
			RecurseConnectionInfoCallback callback = info => info.Clear(type);
			RecurseConnectionInfo(sourceDevice, output, type, callback);
		}

		/// <summary>
		/// Recurses forwards from the device, setting the source on each connection usage.
		/// Clears room ownership if the source changes.
		/// </summary>
		/// <param name="sourceDevice"></param>
		/// <param name="output"></param>
		/// <param name="type"></param>
		/// <param name="source"></param>
		private void PropogateConnectionSource(IRouteSourceControl sourceDevice, int output, eConnectionType type,
		                                       EndpointInfo? source)
		{
			RecurseConnectionInfoCallback callback = info =>
			                                         {
				                                         EndpointInfo? current = info.GetSource(type);

				                                         // Check to see if the sources are the same, regardless of id
				                                         if (source == current)
					                                         return false;

				                                         // Sources are different, clear the rooms
				                                         info.Clear(type);
				                                         info.SetSource(source, type);
				                                         return true;
			                                         };

			RecurseConnectionInfo(sourceDevice, output, type, callback);
		}

		/// <summary>
		/// Recurses forwards from the source, following active switcher routing, calling the callback for each connection.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="output"></param>
		/// <param name="type"></param>
		/// <param name="callback"></param>
		private void RecurseConnectionInfo(IRouteSourceControl sourceControl, int output, eConnectionType type,
		                                   RecurseConnectionInfoCallback callback)
		{
			RecurseConnectionInfo(sourceControl, output, type, callback, new IcdHashSet<Connection>());
		}

		/// <summary>
		/// Recurses forwards from the source, following active switcher routing, calling the callback for each connection.
		/// </summary>
		/// <param name="sourceControl"></param>
		/// <param name="output"></param>
		/// <param name="type"></param>
		/// <param name="callback"></param>
		/// <param name="visited"></param>
		private void RecurseConnectionInfo(IRouteSourceControl sourceControl, int output, eConnectionType type,
		                                   RecurseConnectionInfoCallback callback, IcdHashSet<Connection> visited)
		{
			Connection outputConnection = m_RoutingGraph.Connections.GetOutputConnection(sourceControl, output);
			if (outputConnection == null || visited.Contains(outputConnection))
				return;

			visited.Add(outputConnection);

			// Narrow the connection type
			type = EnumUtils.GetFlagsIntersection(type, outputConnection.ConnectionType);

			ConnectionUsageInfo info = LazyLoadConnectionUsageInfo(outputConnection);

			// Stop recursing if the callback returns false.
			if (!callback(info))
				return;

			IRouteMidpointControl midpointDevice =
				m_RoutingGraph.GetDestinationControl(outputConnection) as IRouteMidpointControl;
			if (midpointDevice == null)
				return;

			// If the next node is a midpoint, loop over the routed outputs and clear connection usages
			foreach (ConnectorInfo switcherOutput in midpointDevice.GetOutputs(outputConnection.Destination.Address, type))
				RecurseConnectionInfo(midpointDevice, switcherOutput.Address, type, callback);
		}

		#endregion

		#region IEnumerable Methods

		public IEnumerator<ConnectionUsageInfo> GetEnumerator()
		{
			m_ConnectionsUsageSection.Enter();

			try
			{
				return m_ConnectionsUsage.OrderValuesByKey()
				                         .ToList()
				                         .GetEnumerator();
			}
			finally
			{
				m_ConnectionsUsageSection.Leave();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
