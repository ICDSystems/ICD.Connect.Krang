using System.Linq;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Cores;

namespace ICD.Connect.Krang.Remote.Direct.Disconnect
{
	public sealed class DisconnectHandler : AbstractMessageHandler<DisconnectMessage, IReply>
	{
		public override IReply HandleMessage(DisconnectMessage message)
		{
			ICore core = ServiceProvider.TryGetService<ICore>();
			if (core == null)
				return null;

			RemoteSwitcher switcher =
				core.Originators.GetChildren<RemoteSwitcher>()
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

	public sealed class DisconnectMessage : AbstractMessage
	{
	}
}
