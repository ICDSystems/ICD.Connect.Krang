using System;
using ICD.Connect.Protocol.Network.Direct;
using Newtonsoft.Json;

namespace ICD.Connect.Krang.Remote.Direct.API
{
	[Serializable]
	public sealed class RemoteApiReply : AbstractApiMessage, IReply
	{
		/// <summary>
		/// The ID of the initial message that is being replied to.
		/// </summary>
		[JsonProperty("oi", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public Guid OriginalMessageId { get; set; }
	}
}
