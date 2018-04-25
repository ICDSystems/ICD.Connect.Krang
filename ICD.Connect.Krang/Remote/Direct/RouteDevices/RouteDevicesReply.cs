﻿using ICD.Connect.Protocol.Network.Direct;

namespace ICD.Connect.Krang.Remote.Direct.RouteDevices
{
	public sealed class RouteDevicesReply : AbstractMessage, IReply
	{
		public bool Result { get; set; }
	}
}