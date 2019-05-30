using System;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Device;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusRemote
{
	public sealed class KrangAtHomeRemoteUi : AbstractKrangAtHomeUi<KrangAtHomeSPlusRemoteDevice>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangAtHomeRemoteUi(KrangAtHomeTheme theme, KrangAtHomeSPlusRemoteDevice uiDevice) : base(theme, uiDevice)
		{
			Subscribe(UiDevice);
		}

		protected override float IncrementValue { get { return 2; } }

		protected override void RaiseRoomInfo()
		{
			UiDevice.RaiseRoomChanged(Room);
		}

		/// <summary>
		/// Raised when source/s become actively/inactively routed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		protected override void RoomOnActiveSourcesChange(object sender, EventArgs eventArgs)
		{
			UiDevice.RaiseSourceChanged(Room.GetSource());
		}

		protected override void InstantiateVolumeControl(IVolumeDeviceControl volumeDevice)
		{
			//Remote doesn't do volume feedback
		}
	}
}