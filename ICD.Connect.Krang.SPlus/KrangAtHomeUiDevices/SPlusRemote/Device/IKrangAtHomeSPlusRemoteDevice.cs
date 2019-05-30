using ICD.Connect.API.Attributes;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Proxy;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Device
{
	public interface IKrangAtHomeSPlusRemoteDevice: IKrangAtHomeUiDevice
	{
		[ApiMethod(SPlusRemoteApi.METHOD_RAISE_ROOM_CHANGED, SPlusRemoteApi.HELP_METHOD_RAISE_ROOM_CHANGED)]
		void RaiseRoomChanged(RoomInfo roomInfo);

		[ApiMethod(SPlusRemoteApi.METHOD_RAISE_SOURCE_CHANGED, SPlusRemoteApi.HELP_METHOD_RAISE_SOURCE_CHANGED)]
		void RaiseSourceChanged(SourceInfo sourceInfo);
	}
}