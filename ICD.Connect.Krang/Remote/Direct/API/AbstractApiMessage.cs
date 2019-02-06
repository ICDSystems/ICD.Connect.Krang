using ICD.Connect.API.Info;
using ICD.Connect.Protocol.Network.Direct;
using Newtonsoft.Json;

namespace ICD.Connect.Krang.Remote.Direct.API
{
	public abstract class AbstractApiMessage : AbstractMessage
	{
		[JsonProperty("c")]
		public ApiClassInfo Command { get; set; }
	}
}
