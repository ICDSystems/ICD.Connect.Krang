using ICD.Common.Utils.Services;
using ICD.Connect.Devices;
using ICD.Connect.Krang.Remote.Direct;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote
{
	public sealed class RemoteSwitcher : AbstractDevice<RemoteSwitcherSettings>
	{
		private HostInfo m_HostInfo;
		private readonly RemoteSwitcherControl m_SwitcherControl;

		#region Properties

		public HostInfo HostInfo { get { return m_HostInfo; } set { m_HostInfo = value; } }

		public bool HasHostInfo { get { return m_HostInfo != default(HostInfo); } }

		public RemoteSwitcherControl SwitcherControl { get { return m_SwitcherControl; } }

		#endregion

		#region Constructor

		public RemoteSwitcher()
		{
			Name = "Remote Switcher";

			m_SwitcherControl = new RemoteSwitcherControl(this);
			Controls.Add(m_SwitcherControl);
		}

		#endregion

		#region Methods

		protected override bool GetIsOnlineStatus()
		{
			return true;
		}

		#endregion

		#region Settings

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			HostInfo = default(HostInfo);
		}

		protected override void CopySettingsFinal(RemoteSwitcherSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Address = HostInfo;
		}

		protected override void ApplySettingsFinal(RemoteSwitcherSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			HostInfo = settings.Address;
		}

		#endregion

		protected override void DisposeFinal(bool disposing)
		{
			if (!HasHostInfo)
				return;
			DirectMessageManager dm = ServiceProvider.GetService<DirectMessageManager>();
			dm.Send(HostInfo, new DisconnectMessage());
			base.DisposeFinal(disposing);
		}
	}
}
