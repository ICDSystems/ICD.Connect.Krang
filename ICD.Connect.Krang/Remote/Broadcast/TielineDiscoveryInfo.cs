using System;
using System.Collections.Generic;
using ICD.Connect.Routing.Connections;

namespace ICD.Connect.Krang.Remote.Broadcast
{
	[Serializable]
	public sealed class TielineDiscoveryInfo
	{
		public Dictionary<int, int> DeviceIds { get; set; }
		public Dictionary<int, IEnumerable<Connection>> Tielines { get; set; }

		public TielineDiscoveryInfo(Dictionary<int, int> deviceIds, Dictionary<int, IEnumerable<Connection>> tielines)
		{
			DeviceIds = deviceIds;
			Tielines = tielines;
		}
	}
}
