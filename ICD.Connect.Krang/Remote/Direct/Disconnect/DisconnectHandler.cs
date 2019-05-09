using System;
using System.Linq;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Krang.Devices;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Cores;

namespace ICD.Connect.Krang.Remote.Direct.Disconnect
{
	public sealed class DisconnectHandler : AbstractMessageHandler
	{
		/// <summary>
		/// Gets the message type that this handler is expecting.
		/// </summary>
		public override Type MessageType { get { return typeof(DisconnectData); } }

		public override Message HandleMessage(Message message)
		{
			ICore core = ServiceProvider.TryGetService<ICore>();
			if (core == null)
				return null;

			RemoteSwitcher switcher =
				core.Originators.GetChildren<RemoteSwitcher>()
				    .SingleOrDefault(rs => rs.HasHostInfo && rs.HostInfo == message.From);
			if (switcher == null)
				return null;

			ServiceProvider.TryGetService<ILoggerService>()
			               .AddEntry(eSeverity.Error,
			                         "Remote Krang at {0} disconnected; RemoteSwitcher {1} going to discovery mode",
			                         message.From, switcher.Id);
			switcher.HostInfo = default(HostSessionInfo);

			return null;
		}
	}
}
