using ICD.Connect.API.Info;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Proxy;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Device;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Proxy
{
	public sealed class ProxySPlusRemoteDevice : AbstractProxySPlusUiDevice<ProxySPlusRemoteDeviceSettings>, IKrangAtHomeSPlusRemoteDevice
	{
		public void RaiseRoomChanged(RoomInfo roomInfo)
		{
			CallMethod(SPlusRemoteApi.METHOD_RAISE_ROOM_CHANGED, roomInfo);
		}

		public void RaiseSourceChanged(SourceInfo sourceInfo)
		{
			CallMethod(SPlusRemoteApi.METHOD_RAISE_SOURCE_CHANGED, sourceInfo);
		}
	}
}