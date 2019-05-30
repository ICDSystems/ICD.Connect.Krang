using System;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.EventArgs;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Device
{
	public interface IKrangAtHomeSPlusRemoteDeviceShimmable : IKrangAtHomeUiDeviceShimmable
	{
		event EventHandler<RoomChangedEventArgs> OnRoomChanged;

		event EventHandler<SourceChangedEventArgs> OnSourceChanged;
	}
}