using System;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.EventArgs;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Device
{
	public sealed class KrangAtHomeSPlusRemoteDevice : AbstractKrangAtHomeUiDevice<KrangAtHomeSPlusRemoteDeviceSettings>, IKrangAtHomeSPlusRemoteDeviceShimmable, IKrangAtHomeSPlusRemoteDevice
	{
		public event EventHandler<RoomChangedEventArgs> OnRoomChanged;

		public event EventHandler<SourceChangedEventArgs> OnSourceChanged;

		public void RaiseRoomChanged(RoomInfo room)
		{
			OnRoomChanged.Raise(this, new RoomChangedEventArgs(room));
		}

		public void RaiseSourceChanged(SourceInfo source)
		{
			OnSourceChanged.Raise(this, new SourceChangedEventArgs(source));
		}

		public override float IncrementValue { get { return 0.03f; } }

		protected override void InstantiateVolumeControl(IVolumeDeviceControl volumeDevice)
		{
			//Remote doesn't do volume feedback
		}
	}
}