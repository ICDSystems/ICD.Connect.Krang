using System;
using System.Collections.Generic;
using ICD.Connect.Routing.Connections;

namespace ICD.Connect.Krang.Remote.Broadcast
{
	[Serializable]
	public sealed class TielineDiscoveryData
	{
		public Dictionary<int, int> DeviceIds { get; set; }
		public Dictionary<int, IEnumerable<Connection>> Tielines { get; set; }

		public TielineDiscoveryData(Dictionary<int, int> deviceIds, Dictionary<int, IEnumerable<Connection>> tielines)
		{
			DeviceIds = deviceIds;
			Tielines = tielines;
		}
	}
}
