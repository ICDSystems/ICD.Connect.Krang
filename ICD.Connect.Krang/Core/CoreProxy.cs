using ICD.Common.Utils.Services;
using ICD.Connect.API;
using ICD.Connect.API.Info;
using ICD.Connect.Krang.Remote.Direct.API;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Core
{
	/// <summary>
	/// CoreProxy simply represents an abstract core instance that lives remotely.
	/// </summary>
	public sealed class CoreProxy : AbstractCore<CoreProxySettings>
	{
		private DirectMessageManager DirectMessageManager { get { return ServiceProvider.GetService<DirectMessageManager>(); } }

		private RemoteApiResultHandler ApiResultHandler { get { return ServiceProvider.GetService<RemoteApiResultHandler>(); } }

		public void SetHostInfo(HostInfo source)
		{
			ApiClassInfo command =
				ApiCommandBuilder.NewCommand()
				                 .AtNode("ControlSystem")
				                 .AtNode("Core")
				                 .AtNodeGroup("Devices")
								 .Complete();

			RemoteApiMessage message = new RemoteApiMessage
			{
				Command = command
			};

			DirectMessageManager.Send(source, message);
		}
	}
}
