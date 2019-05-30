using System;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Device;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusRemote
{
	public sealed class KrangAtHomeRemoteUi : AbstractKrangAtHomeUi<IKrangAtHomeSPlusRemoteDevice>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangAtHomeRemoteUi(KrangAtHomeTheme theme, IKrangAtHomeSPlusRemoteDevice uiDevice) : base(theme, uiDevice)
		{
			Subscribe(UiDevice);
		}

		protected override void RaiseRoomInfo()
		{
			UiDevice.RaiseRoomChanged(new RoomInfo(Room));
		}

		/// <summary>
		/// Raised when source/s become actively/inactively routed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		protected override void RoomOnActiveSourcesChange(object sender, EventArgs eventArgs)
		{
			UiDevice.RaiseSourceChanged(new SourceInfo(Room.GetSource()));
		}
	}
}