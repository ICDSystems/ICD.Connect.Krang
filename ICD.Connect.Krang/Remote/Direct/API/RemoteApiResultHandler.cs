using ICD.Connect.Protocol.Network.Direct;

namespace ICD.Connect.Krang.Remote.Direct.API
{
	public delegate void RemoteApiReplyCallback(RemoteApiResultHandler sender, RemoteApiReply reply);

	public sealed class RemoteApiResultHandler : AbstractMessageHandler<RemoteApiReply, IReply>
	{
		/// <summary>
		/// Raised when we receive an API reply from an endpoint.
		/// </summary>
		public event RemoteApiReplyCallback OnApiResult;

		public override IReply HandleMessage(RemoteApiReply message)
		{
			RemoteApiReplyCallback handler = OnApiResult;
			if (handler != null)
				handler(this, message);

			return null;
		}

		protected override void Dispose(bool disposing)
		{
			OnApiResult = null;

			base.Dispose(disposing);
		}
	}
}
