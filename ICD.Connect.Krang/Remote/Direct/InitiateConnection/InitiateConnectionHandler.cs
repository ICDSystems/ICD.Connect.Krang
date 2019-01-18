using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Settings.Cores;

namespace ICD.Connect.Krang.Remote.Direct.InitiateConnection
{
	public sealed class InitiateConnectionHandler : AbstractMessageHandler<InitiateConnectionMessage, IReply>
	{
		public override IReply HandleMessage(InitiateConnectionMessage message)
		{
			ICore core = ServiceProvider.GetService<ICore>();
			RemoteSwitcher switcher = (RemoteSwitcher)core.Originators.GetChild(message.DeviceId);
			switcher.HostInfo = message.MessageFrom;
			ServiceProvider.TryGetService<ILoggerService>()
			               .AddEntry(eSeverity.Informational, "Remote Krang discovered for device id {0} at {1}",
			                         message.DeviceId, message.MessageFrom);
			return null;
		}
	}
}
