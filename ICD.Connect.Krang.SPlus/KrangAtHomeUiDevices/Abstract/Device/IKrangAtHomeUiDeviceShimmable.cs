using ICD.Connect.API.Attributes;
using ICD.Connect.Devices.Simpl;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Proxy;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device
{
	public interface IKrangAtHomeUiDeviceShimmable : ISimplDevice
	{

		#region Methods

		[ApiMethod(SPlusUiDeviceApi.METHOD_SET_ROOM_ID, SPlusUiDeviceApi.HELP_METHOD_SET_ROOM_ID)]
		void SetRoomId(int id);

		[ApiMethod(SPlusUiDeviceApi.METHOD_SET_AUDIO_SOURCE_ID, SPlusUiDeviceApi.HELP_METHOD_SET_AUDIO_SOURCE_ID)]
		void SetAudioSourceId(int id);

		[ApiMethod(SPlusUiDeviceApi.METHOD_SET_VIDEO_SOURCE_ID, SPlusUiDeviceApi.HELP_METHOD_SET_VIDEO_SOURCE_ID)]
		void SetVideoSourcdId(int id);

		[ApiMethod(SPlusUiDeviceApi.METHOD_SET_VOLUME_LEVEL, SPlusUiDeviceApi.HELP_METHOD_SET_VOLUME_LEVEL)]
		void SetVolumeLevel(float volumeLevel);

		[ApiMethod(SPlusUiDeviceApi.METHOD_SET_VOLUME_RAMP_UP, SPlusUiDeviceApi.HELP_METHOD_SET_VOLUME_RAMP_UP)]
		void SetVolumeRampUp();

		[ApiMethod(SPlusUiDeviceApi.METHOD_SET_VOLUME_RAMP_DOWN, SPlusUiDeviceApi.HELP_METHOD_SET_VOLUME_RAMP_DOWN)]
		void SetVolumeRampDown();

		[ApiMethod(SPlusUiDeviceApi.METHOD_SET_VOLUME_RAMP_STOP, SPlusUiDeviceApi.HELP_METHOD_SET_VOLUME_RAMP_STOP)]
		void SetVolumeRampStop();

		[ApiMethod(SPlusUiDeviceApi.METHOD_SET_VOLUME_MUTE, SPlusUiDeviceApi.HELP_METHOD_SET_VOLUME_MUTE)]
		void SetVolumeMute(bool state);

		[ApiMethod(SPlusUiDeviceApi.METHOD_SET_VOLUME_MUTE_TOGGLE, SPlusUiDeviceApi.HELP_METHOD_SET_VOLUME_MUTE_TOGGLE)]
		void SetVolumeMuteToggle();

		#endregion
	}
}