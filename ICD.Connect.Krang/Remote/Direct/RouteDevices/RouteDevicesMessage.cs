using System;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Routing;

namespace ICD.Connect.Krang.Remote.Direct.RouteDevices
{
	[Serializable]
	public sealed class RouteDevicesMessage : AbstractMessage
	{
		public RouteOperation Operation { get; set; }

		public RouteDevicesMessage()
		{
		}

		public RouteDevicesMessage(RouteOperation op)
		{
			Operation = op;
		}
	}
}
