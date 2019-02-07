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
		public KrangAtHomeRemoteUi(KrangAtHomeTheme theme, KrangAtHomeSPlusRemoteDevice panel) : base(theme, panel)
		{
		}

		protected override void RaiseRoomInfo()
		{
			Panel.RaiseRoomChanged(Room);
		}

		/// <summary>
		/// Raised when source/s become actively/inactively routed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		protected override void RoomOnActiveSourcesChange(object sender, EventArgs eventArgs)
		{
			Panel.RaiseSourceChanged(Room.GetSource());
		}

		protected override void InstantiateVolumeControl(IVolumeDeviceControl volumeDevice)
		{
			//Remote doesn't do volume feedback
		}
	}
}