using System;
using ICD.Connect.Protocol.Network.Direct;

namespace ICD.Connect.Krang.Remote.Direct.InitiateConnection
{
	[Serializable]
	public sealed class InitiateConnectionMessage : AbstractMessage
	{
		public int DeviceId { get; set; }
	}
}
