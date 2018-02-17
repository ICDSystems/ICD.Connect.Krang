using ICD.Connect.API;
using ICD.Connect.Protocol.Network.Direct;

namespace ICD.Connect.Krang.Remote.Direct.API
{
	public sealed class RemoteApiCommandHandler : AbstractMessageHandler<RemoteApiMessage>
	{
		protected override AbstractMessage HandleMessage(RemoteApiMessage message)
		{
			ApiHandler.HandleRequest(message.Command);
			return new RemoteApiReply {Command = message.Command};
		}
	}
}
