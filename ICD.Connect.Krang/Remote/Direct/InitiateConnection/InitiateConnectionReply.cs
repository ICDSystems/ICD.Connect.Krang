using System.Collections.Generic;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Routing.Connections;

namespace ICD.Connect.Krang.Remote.Direct.InitiateConnection
{
	public class InitiateConnectionReply : AbstractMessage
	{
		public int DeviceId { get; set; }

		public List<Connection> Tielines { get; set; }

		public HostInfo HostInfo { get; set; }
	}
}
