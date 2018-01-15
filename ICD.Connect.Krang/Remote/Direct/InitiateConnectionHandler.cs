using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote.Direct
{
	public sealed class InitiateConnectionHandler : AbstractMessageHandler<InitiateConnectionMessage>
	{
		public override AbstractMessage HandleMessage(InitiateConnectionMessage message)
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
