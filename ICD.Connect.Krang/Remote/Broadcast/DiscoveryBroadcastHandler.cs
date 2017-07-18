using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices.Extensions;
using ICD.Connect.Krang.Remote.Direct;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Extensions;
using ICD.Connect.Routing.Mock.Destination;
using ICD.Connect.Routing.Mock.Source;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote.Broadcast
{
	public sealed class DiscoveryBroadcastHandler
	{
		private readonly RecurringBroadcast<KrangDiscoveryBroadcast> m_Broadcast;
		private readonly ICore m_Core;
		private readonly BroadcastManager m_BroadcastManager;
		private readonly DirectMessageManager m_DirectMessageManager;

		public DiscoveryBroadcastHandler()
		{
			m_Broadcast = new RecurringBroadcast<KrangDiscoveryBroadcast>();
			m_Broadcast.OnBroadcasting += UpdateData;
			m_Broadcast.OnBroadcastReceived += HandleBroadcast;

			m_BroadcastManager = ServiceProvider.GetService<BroadcastManager>();
			m_BroadcastManager.RegisterBroadcast(m_Broadcast);

			m_DirectMessageManager = ServiceProvider.GetService<DirectMessageManager>();

			m_Core = ServiceProvider.GetService<ICore>();
		}

		private void UpdateData(object sender, EventArgs e)
		{
			int[] remoteSwitchers =
				m_Core.Originators.OfType<RemoteSwitcher>().Where(d => !d.HasHostInfo).Select(d => d.Id).ToArray();
			List<Connection> connections = m_Core.GetRoutingGraph().Connections.GetConnections().ToList();

			Dictionary<int, int> devices = new Dictionary<int, int>();
			Dictionary<int, IEnumerable<Connection>> deviceConnections = new Dictionary<int, IEnumerable<Connection>>();

			foreach (int id in remoteSwitchers)
			{
				List<Connection> tielines = connections.Where(c => c.Source.Device == id || c.Destination.Device == id).ToList();
				int id1 = id;
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
			m_Broadcast.UpdateData(devices.Any() ? new KrangDiscoveryBroadcast(devices, deviceConnections) : null);
		}

		private void HandleBroadcast(object sender, BroadcastEventArgs<KrangDiscoveryBroadcast> e)
		{
			if (e.Data.Source == m_BroadcastManager.GetHostInfo())
				return;
			foreach (KeyValuePair<int, int> pair in e.Data.Data.DeviceIds)
			{
				if (!m_Core.Originators.ContainsChild(pair.Key) || m_Core.Originators.GetChild(pair.Key) is RemoteSwitcher)
					continue;
				if (!m_Core.Originators.ContainsChild(pair.Value))
				{
					RemoteSwitcher switcher = new RemoteSwitcher {Id = pair.Value, HostInfo = e.Data.Source};
					m_Core.Originators.AddChild(switcher);
				}
				else
				{
					RemoteSwitcher switcher = m_Core.Originators.GetChild(pair.Value) as RemoteSwitcher;
					if (switcher != null)
						switcher.HostInfo = e.Data.Source;
				}

				List<Connection> connections = m_Core.GetRoutingGraph().Connections.ToList();
				foreach (Connection tieline in e.Data.Data.Tielines[pair.Key])
				{
					if (connections.All(c => c.Id != tieline.Id))
						connections.Add(tieline);
				}
				m_Core.GetRoutingGraph().Connections.SetConnections(connections);

				HostInfo hostInfo = m_DirectMessageManager.GetHostInfo();
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Informational,
				                         "Sending response to Krang Discovery Broadcast. Device: {0}, Host: {1}", pair.Key,
				                         e.Data.Source.ToString());
				m_DirectMessageManager.Send(e.Data.Source,
				                            new InitiateConnectionMessage {DeviceId = pair.Key});
			}
		}
	}
}
