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
		public Guid Session { get; set; }
		public DateTime DiscoveryTime { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public CoreDiscoveryInfo(BroadcastData data)
		{
			CoreDiscoveryData discovery = data.Data as CoreDiscoveryData;
			if (discovery == null)
				throw new ArgumentException("Expected " + typeof(CoreDiscoveryData).Name, "data");

			Id = discovery.Id;
			Name = discovery.Name;
			Source = data.Source;
			DiscoveryTime = IcdEnvironment.GetLocalTime();
			Session = data.Session;
		}

		/// <summary>
		/// Returns true if the Ids are the same, but the host info is different.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Conflicts(CoreDiscoveryInfo other)
		{
			if (other == null)
				throw new ArgumentNullException("other");

			if (Id != other.Id)
				return false;

			return Source != other.Source ||
			       Session != other.Session;
		}
	}
}