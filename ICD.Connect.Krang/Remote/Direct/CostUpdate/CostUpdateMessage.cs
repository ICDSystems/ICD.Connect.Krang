﻿using System.Collections.Generic;
using ICD.Connect.Protocol.Network.Direct;

namespace ICD.Connect.Krang.Remote.Direct.CostUpdate
{
	public sealed class CostUpdateMessage : AbstractMessage
	{
		public Dictionary<int, double> SourceCosts { get; set; }
		public Dictionary<int, double> DestinationCosts { get; set; }
	}
}
