using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Krang.Devices;
using ICD.Connect.Krang.Remote.Direct.InitiateConnection;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Broadcast.Broadcasters;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Extensions;
using ICD.Connect.Routing.Mock.Destination;
using ICD.Connect.Routing.Mock.Source;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Settings.Cores;

namespace ICD.Connect.Krang.Remote.Broadcast.TielineDiscovery
{
	public sealed class TielineDiscoveryBroadcastHandler : AbstractBroadcastHandler<TielineDiscoveryData>
	{
		private readonly ICore m_Core;

		/// <summary>
		/// Constructor.
		/// </summary>
		public TielineDiscoveryBroadcastHandler()
		{
			m_Core = ServiceProvider.GetService<ICore>();

			SetBroadcaster(new RecurringBroadcaster<TielineDiscoveryData>());
		}

		protected override void BroadcasterOnBroadcasting(object sender, EventArgs e)
		{
			base.BroadcasterOnBroadcasting(sender, e);

			int[] remoteSwitchers =
				m_Core.Originators
				      .GetChildren<RemoteSwitcher>(d => !d.HasHostInfo)
				      .Select(d => d.Id)
				      .ToArray();

			IRoutingGraph graph = GetRoutingGraph();

			List<Connection> connections =
				graph == null
					? new List<Connection>()
					: graph.Connections
					       .GetChildren()
					       .ToList();

			Dictionary<int, int> devices = new Dictionary<int, int>();
			Dictionary<int, IEnumerable<Connection>> deviceConnections = new Dictionary<int, IEnumerable<Connection>>();

			foreach (int id in remoteSwitchers)
			{
				// Workaround for compiler warning
				int id1 = id;

				List<Connection> tielines = connections.Where(c => c.Source.Device == id1 || c.Destination.Device == id1).ToList();

				int deviceId = tielines.Select(c => c.Source.Device == id1 ? c.Destination.Device : c.Source.Device)
				                       .Where(c =>
				                              !(m_Core.Originators.GetChild(c) is MockSourceDevice) &&
				                              !(m_Core.Originators.GetChild(c) is MockDestinationDevice))
				                       .Unanimous(-1);

				tielines = tielines.Where(c => c.Source.Device == deviceId || c.Destination.Device == deviceId).ToList();

				if (tielines.Any() && deviceId != -1)
				{
					devices.Add(id, deviceId);
					deviceConnections.Add(id, tielines);
				}
			}

			object data = devices.Any()
				              ? new TielineDiscoveryData {DeviceIds = devices, Tielines = deviceConnections}
				              : null;

			Broadcaster.SetBroadcastData(data);
		}

		protected override void BroadcasterOnBroadcastReceived(object sender, BroadcastEventArgs e)
		{
			base.BroadcasterOnBroadcastReceived(sender, e);

			if (e.Data.HostSession == BroadcastManager.GetHostSessionInfo())
				return;

			TielineDiscoveryData data = e.Data.Data as TielineDiscoveryData;
			if (data == null)
				return;

			foreach (KeyValuePair<int, int> pair in data.DeviceIds)
			{
				if (!m_Core.Originators.ContainsChild(pair.Key) || m_Core.Originators.GetChild(pair.Key) is RemoteSwitcher)
					continue;
				if (!m_Core.Originators.ContainsChild(pair.Value))
				{
					RemoteSwitcher switcher = new RemoteSwitcher {Id = pair.Value, HostInfo = e.Data.HostSession};
					m_Core.Originators.AddChild(switcher);
				}
				else
				{
					RemoteSwitcher switcher = m_Core.Originators.GetChild(pair.Value) as RemoteSwitcher;
					if (switcher != null)
						switcher.HostInfo = e.Data.HostSession;
				}

				IRoutingGraph graph = GetRoutingGraph();

				if (graph != null)
				{
					List<Connection> connections = graph.Connections.ToList();
					foreach (Connection tieline in data.Tielines[pair.Key])
					{
						// Workaround for compiler warning
						Connection tieline1 = tieline;

						if (connections.All(c => c.Id != tieline1.Id))
							connections.Add(tieline);
					}
					graph.Connections.SetChildren(connections);
				}

				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Informational,
				                         "Sending response to Krang Discovery Broadcast. Device: {0}, Host: {1}", pair.Key,
				                         e.Data.HostSession.ToString());

				InitiateConnectionData messageData = new InitiateConnectionData {DeviceId = pair.Key};
				Message message = Message.FromData(messageData);

				DirectMessageManager.Send(e.Data.HostSession, message);
			}
		}

		[CanBeNull]
		private IRoutingGraph GetRoutingGraph()
		{
			IRoutingGraph output;
			m_Core.TryGetRoutingGraph(out output);
			return output;
		}
	}
}
