using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Devices;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.EventArgs;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Proxy;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Device
{
	[ApiClass(typeof(ProxySPlusRemoteDevice), typeof(IDevice))]
	public interface IKrangAtHomeSPlusRemoteDeviceShimmable : IKrangAtHomeUiDeviceShimmable
	{
		[ApiEvent(SPlusRemoteApi.EVENT_ROOM_CHANGED, SPlusRemoteApi.EVENT_ROOM_CHANGED_HELP)]
		event EventHandler<RoomChangedApiEventArgs> OnRoomChanged;

		[ApiEvent(SPlusRemoteApi.EVENT_SOURCE_CHANGED, SPlusRemoteApi.EVENT_SOURCE_CHANGED_HELP)]
		event EventHandler<SourceChangedApiEventArgs> OnSourceChanged;
	}
}