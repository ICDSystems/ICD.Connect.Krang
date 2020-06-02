using System;
using ICD.Common.Utils.Services;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Krang.Remote.Direct.Disconnect;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.Devices
{
	public sealed class RemoteSwitcher : AbstractDevice<RemoteSwitcherSettings>
	{
		#region Properties

		public HostSessionInfo HostInfo { get; set; }

		public bool HasHostInfo { get { return HostInfo != default(HostSessionInfo); } }

		public RemoteSwitcherControl SwitcherControl { get { return Controls.GetControl<RemoteSwitcherControl>(); } }

		#endregion

		#region Private Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			if (!HasHostInfo)
				return;

			DirectMessageManager dm = ServiceProvider.GetService<DirectMessageManager>();
			dm.Send(HostInfo, Message.FromData(new DisconnectData()));
			base.DisposeFinal(disposing);
		}

		protected override bool GetIsOnlineStatus()
		{
			return true;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			HostInfo = default(HostSessionInfo);
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(RemoteSwitcherSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Address = HostInfo.Host;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(RemoteSwitcherSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			HostInfo = new HostSessionInfo(settings.Address, new Guid());
		}

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(RemoteSwitcherSettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new RemoteSwitcherControl(this));
		}

		#endregion
	}
}
