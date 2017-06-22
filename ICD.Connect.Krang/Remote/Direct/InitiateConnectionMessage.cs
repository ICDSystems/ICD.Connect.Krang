using System;
using ICD.Connect.Protocol.Network.Direct;

namespace ICD.Connect.Krang.Remote.Direct
{
	[Serializable]
	public class InitiateConnectionMessage : AbstractMessage
	{
		public int DeviceId { get; set; }
	}
}
