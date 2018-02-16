using ICD.Connect.API;
using ICD.Connect.Protocol.Network.Direct;

namespace ICD.Connect.Krang.Remote.Direct.API
{
	public delegate void RemoteApiReplyCallback(RemoteApiHandler sender, RemoteApiReply reply);

	public sealed class RemoteApiHandler : AbstractMessageHandler<AbstractApiMessage>
	{
		/// <summary>
		/// Raised when we receive an API reply from an endpoint.
		/// </summary>
		public event RemoteApiReplyCallback OnApiReply;

		protected override AbstractMessage HandleMessage(AbstractApiMessage message)
		{
			// Incoming requests are handled by the API.
			if (message is RemoteApiMessage)
			{
				ApiHandler.HandleRequest(message.Command);
				return new RemoteApiReply {Command = message.Command};
			}

			// Incoming responses are raised to be interpreted by the program.
			if (message is RemoteApiReply)
			{
				RemoteApiReplyCallback handler = OnApiReply;
				if (handler != null)
					handler(this, message as RemoteApiReply);
			}

			return null;
		}

		protected override void Dispose(bool disposing)
		{
			OnApiReply = null;

			base.Dispose(disposing);
		}
	}
}
