using ICD.Common.Utils;
using ICD.Common.Utils.Json;
using ICD.Connect.API;
using ICD.Connect.API.Info;
using ICD.Connect.Protocol.Network.Direct;

namespace ICD.Connect.Krang.Remote.Direct.API
{
	/// <summary>
	/// The RemoteApiCommandHandler receives 
	/// </summary>
	public sealed class RemoteApiCommandHandler : AbstractMessageHandler<RemoteApiMessage, RemoteApiReply>
	{
		/// <summary>
		/// Handles the message receieved
		/// </summary>
		/// <param name="message"></param>
		/// <returns>Returns an AbstractMessage as a reply, or null for no reply</returns>
		protected override RemoteApiReply HandleMessage(RemoteApiMessage message)
		{
			ApiHandler.HandleRequest(this, message.Command);
			return new RemoteApiReply {Command = message.Command};
		}

		/// <summary>
		/// Handles an event feedback command from the ApiHandler.
		/// </summary>
		/// <param name="command"></param>
		public void HandleFeedback(ApiClassInfo command)
		{
			IcdConsole.PrintLine(eConsoleColor.Magenta, "API event raised - {0}", JsonUtils.Format(command));
		}
	}
}
