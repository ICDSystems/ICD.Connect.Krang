﻿using System.Collections.Generic;
using ICD.Connect.Protocol.Network.Direct;

namespace ICD.Connect.Krang.Remote.Direct.RequestDevices
{
	public sealed class RequestDevicesMessage : AbstractMessage
	{
		public IEnumerable<int> Sources { get; set; }
		public IEnumerable<int> Destinations { get; set; }
	}
}
