using System;
using System.Collections.Generic;
using System.Linq;
using ICD.SimplSharp.API.Devices;
using ICD.SimplSharp.API.Ports;
using ICD.SimplSharp.KrangLib.Core;
using ICD.SimplSharp.KrangLib.Remote.Direct;
using ICD.SimplSharp.Network.Broadcast;

namespace ICD.SimplSharp.KrangLib.Remote.Broadcast
{
	public class KrangDiscoveryBroadcastHandler
	{
		private readonly RecurringBroadcast<KrangDiscoveryBroadcast> m_Broadcast;
		private readonly Krang m_Krang;

		public KrangDiscoveryBroadcastHandler(Krang krang)
		{
			m_Broadcast = new RecurringBroadcast<KrangDiscoveryBroadcast>();
			m_Broadcast.OnBroadcasting += UpdateData;
			m_Broadcast.OnBroadcastReceived += HandleBroadcast;
			krang.BroadcastManager.RegisterBroadcast(m_Broadcast);
			m_Krang = krang;
		}

		private void UpdateData(object sender, EventArgs e)
		{
			int[] deviceIds = m_Krang.GetDevices().Where(d => d is RemoteSwitcher).Cast<RemoteSwitcher>().Where(d => d.HasHostInfo).Select(d => d.Id).ToArray();
			m_Broadcast.UpdateData(new KrangDiscoveryBroadcast(deviceIds));
		}

		private void HandleBroadcast(object sender, BroadcastEventArgs<KrangDiscoveryBroadcast> e)
		{
			if (sender == m_Broadcast)
				return;
			int[] deviceIds = e.Data.Data.DeviceIds;
			IEnumerable<IDevice> matchingDevices = m_Krang.GetDevices().Where(d => deviceIds.Contains(d.Id));

			HostInfo hostInfo = m_Krang.BroadcastManager.GetHostInfo();
			foreach (IDevice device in matchingDevices)
			{
				m_Krang.DirectMessageManager.Send<InitiateConnectionReply>(e.Data.Source, new InitiateConnectionMessage() {DeviceId = device.Id, Owner = hostInfo}, ConnectedToRemoteKrang);
			}
		}

		private void ConnectedToRemoteKrang(InitiateConnectionReply response)
		{
			foreach (var connection in response.Connections)
			{
				if (m_Krang.GetDevice(connection.SourceId) == null)
					CreateAndAddNewRemoteSwitcher(connection.SourceId, response.HostInfo);
				else if (m_Krang.GetDevice(connection.DestinationId) == null)
					CreateAndAddNewRemoteSwitcher(connection.DestinationId, response.HostInfo);
			}
			var connections = m_Krang.RoutingGraph.GetConnections().ToList();
			connections.AddRange(response.Connections);
			m_Krang.RoutingGraph.SetConnections(connections);
		}

		private void CreateAndAddNewRemoteSwitcher(int id, HostInfo address)
		{
			RemoteSwitcher switcher = new RemoteSwitcher { Id = id, HostInfo = address };
			m_Krang.AddDevice(switcher);
		}
	}
}