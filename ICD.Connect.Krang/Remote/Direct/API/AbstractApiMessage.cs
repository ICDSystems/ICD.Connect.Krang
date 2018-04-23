using ICD.Connect.API.Info;
using ICD.Connect.Protocol.Network.Direct;

namespace ICD.Connect.Krang.Remote.Direct.API
{
	public abstract class AbstractApiMessage : AbstractMessage
	{
		public ApiClassInfo Command { get; set; }
	}
}
