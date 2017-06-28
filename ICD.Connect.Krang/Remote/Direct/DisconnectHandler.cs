using System.Linq;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Connect.Devices.Extensions;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote.Direct
{
	public class DisconnectHandler : AbstractMessageHandler<DisconnectMessage>
	{
		public override AbstractMessage HandleMessage(DisconnectMessage message)
		{
			ICore core = ServiceProvider.TryGetService<ICore>();
			if (core == null)
				return null;

			RemoteSwitcher switcher =
				core.GetDevices().GetChildren<RemoteSwitcher>()
				    .SingleOrDefault(rs => rs.HasHostInfo && rs.HostInfo == message.MessageFrom);
			if (switcher == null)
				return null;

			ServiceProvider.TryGetService<ILoggerService>()
			               .AddEntry(eSeverity.Error,
			                         "Remote Krang at {0} disconnected; RemoteSwitcher {1} going to discovery mode",
			                         message.MessageFrom, switcher.Id);
			switcher.HostInfo = default(HostInfo);

			return null;
		}
	}

	public class DisconnectMessage : AbstractMessage
	{
	}
}
