using System;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Krang.Remote.Broadcast.CoreDiscovery
{
	public sealed class CoreDiscoveryInfo
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public HostInfo Source { get; set; }
		public DateTime Discovered { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public CoreDiscoveryInfo(BroadcastData data)
		{
			CoreDiscoveryData discovery = data.Data as CoreDiscoveryData;
			if (discovery == null)
				throw new InvalidOperationException();

			Id = discovery.Id;
			Name = discovery.Name;
			Source = data.Source;
			Discovered = IcdEnvironment.GetLocalTime();
		}

		/// <summary>
		/// Returns true if the Ids are the same, but the host info is different.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Conflicts(CoreDiscoveryInfo other)
		{
			return Id == other.Id && Source != other.Source;
		}
	}
}