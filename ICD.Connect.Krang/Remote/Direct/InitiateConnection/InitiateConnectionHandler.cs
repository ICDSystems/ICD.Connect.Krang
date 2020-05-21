using System;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Krang.Devices;
using ICD.Connect.Protocol.Network.Direct;

namespace ICD.Connect.Krang.Remote.Direct.InitiateConnection
{
	public sealed class InitiateConnectionHandler : AbstractMessageHandler
	{
		/// <summary>
		/// Gets the message type that this handler is expecting.
		/// </summary>
		public override Type MessageType { get { return typeof(InitiateConnectionData); } }

		public override Message HandleMessage(Message message)
		{
			InitiateConnectionData data = message.Data as InitiateConnectionData;
			if (data == null)
				return null;

			RemoteSwitcher switcher = (RemoteSwitcher)Core.Originators.GetChild(data.DeviceId);
			switcher.HostInfo = message.From;
			ServiceProvider.TryGetService<ILoggerService>()
			               .AddEntry(eSeverity.Informational, "Remote Krang discovered for device id {0} at {1}",
									 data.DeviceId, message.From);
			return null;
		}
	}
}
