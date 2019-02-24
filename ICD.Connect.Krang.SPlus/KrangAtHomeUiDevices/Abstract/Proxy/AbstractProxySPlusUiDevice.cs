using ICD.Connect.Devices.Proxies.Devices;
using ICD.Connect.Devices.Simpl;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Proxy
{
	public abstract class AbstractProxySPlusUiDevice<TSettings> : AbstractSimplProxyDevice<TSettings>
		where TSettings : IProxyDeviceSettings
	{
		public void SetRoomId(int id)
		{
			CallMethod(SPlusUiDeviceApi.METHOD_SET_ROOM_ID, id);
		}

		public void SetAudioSourceId(int id)
		{
			CallMethod(SPlusUiDeviceApi.METHOD_SET_AUDIO_SOURCE_ID, id);
		}

		public void SetVideoSourcdId(int id)
		{
			CallMethod(SPlusUiDeviceApi.METHOD_SET_VIDEO_SOURCE_ID, id);
		}

		public void SetVolumeLevel(float volumeLevel)
		{
			CallMethod(SPlusUiDeviceApi.METHOD_SET_VOLUME_LEVEL, volumeLevel);
		}

		public void SetVolumeRampUp()
		{
			CallMethod(SPlusUiDeviceApi.METHOD_SET_VOLUME_RAMP_UP);
		}

		public void SetVolumeRampDown()
		{
			CallMethod(SPlusUiDeviceApi.METHOD_SET_VOLUME_RAMP_DOWN);
		}

		public void SetVolumeRampStop()
		{
			CallMethod(SPlusUiDeviceApi.METHOD_SET_VOLUME_RAMP_STOP);
		}

		public void SetVolumeMute(bool state)
		{
			CallMethod(SPlusUiDeviceApi.METHOD_SET_VOLUME_MUTE, state);
		}

		public void SetVolumeMuteToggle()
		{
			CallMethod(SPlusUiDeviceApi.METHOD_SET_VOLUME_MUTE_TOGGLE);
		}
	}
}