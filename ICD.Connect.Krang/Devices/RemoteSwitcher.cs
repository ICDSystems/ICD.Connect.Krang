﻿using System;
using ICD.Common.Utils.Services;
using ICD.Connect.Devices;
using ICD.Connect.Krang.Remote.Direct.Disconnect;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.Devices
{
	public sealed class RemoteSwitcher : AbstractDevice<RemoteSwitcherSettings>
	{
		private readonly RemoteSwitcherControl m_SwitcherControl;

		#region Properties

		public HostSessionInfo HostInfo { get; set; }

		public bool HasHostInfo { get { return HostInfo != default(HostSessionInfo); } }

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

			HostInfo = default(HostSessionInfo);
		}

		protected override void CopySettingsFinal(RemoteSwitcherSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Address = HostInfo.Host;
		}

		protected override void ApplySettingsFinal(RemoteSwitcherSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			HostInfo = new HostSessionInfo(settings.Address, new Guid());
		}

		#endregion

		protected override void DisposeFinal(bool disposing)
		{
			if (!HasHostInfo)
				return;

			DirectMessageManager dm = ServiceProvider.GetService<DirectMessageManager>();
			dm.Send(HostInfo, Message.FromData(new DisconnectData()));
			base.DisposeFinal(disposing);
		}
	}
}
