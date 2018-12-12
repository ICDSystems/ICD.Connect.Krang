using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Simpl;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.EventArgs;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.Proxy;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.Device
{
	[ApiClass(typeof(ProxySPlusTouchpanelDevice),typeof(IDevice))]
	public interface IKrangAtHomeSPlusTouchpanelDevice : ISimplDevice
	{
		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_ROOM_LIST, SPlusTouchpanelDeviceApi.HELP_ROOM_LIST_EVENT)]
		event EventHandler<RoomListEventArgs> OnRoomListUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_ROOM_SELECTED, SPlusTouchpanelDeviceApi.HELP_ROOM_SELECTED_EVENT)]
		event EventHandler<RoomSelectedEventArgs> OnRoomSelectedUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_AUDIO_SOURCE_LIST, SPlusTouchpanelDeviceApi.HELP_AUDIO_SOURCE_LIST_EVENT)]
		event EventHandler<AudioSourceListEventArgs> OnAudioSourceListUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_VIDEO_SOURCE_LIST, SPlusTouchpanelDeviceApi.HELP_VIDEO_SOURCE_LIST_EVENT)]
		event EventHandler<VideoSourceListEventArgs> OnVideoSourceListUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_SOURCE_SELECTED, SPlusTouchpanelDeviceApi.HELP_SOURCE_INFO_EVENT)]
		event EventHandler<SourceSelectedEventArgs> OnSourceSelectedUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_VOLUME_LEVEL_FEEDBACK, SPlusTouchpanelDeviceApi.HELP_VOLUME_LEVEL_FEEDBACK_EVENT)]
		event EventHandler<VolumeLevelFeedbackEventArgs> OnVolumeLevelFeedbackUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_VOLUME_MUTE_FEEDBACK, SPlusTouchpanelDeviceApi.HELP_VOLUME_MUTE_FEEDBACK_EVENT)]
		event EventHandler<VolumeMuteFeedbackEventArgs> OnVolumeMuteFeedbackUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_VOLUME_AVAILABLE_CONTROL, SPlusTouchpanelDeviceApi.HELP_VOLUME_AVAILABLE_CONTROL_EVENT)]
		event EventHandler<VolumeAvailableControlEventArgs> OnVolumeAvailableControlUpdate;

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_REQUEST_REFRESH, SPlusTouchpanelDeviceApi.HELP_METHOD_REQUEST_REFRESH)]
		void RequestDeviceRefresh();

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_ROOM_INDEX, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_ROOM_INDEX)]
		void SetRoomIndex(int index);

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_ROOM_ID, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_ROOM_ID)]
		void SetRoomId(int id);

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_AUDIO_SOURCE_INDEX, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_AUDIO_SOURCE_INDEX)]
		void SetAudioSourceIndex(int index);

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_AUDIO_SOURCE_ID, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_AUDIO_SOURCE_ID)]
		void SetAudioSourceId(int id);

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VIDEO_SOURCE_INDEX, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_VIDEO_SOURCE_INDEX)]
		void SetVideoSourceIndex(int index);

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VIDEO_SOURCE_ID, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_VIDEO_SOURCE_ID)]
		void SetVideoSourcdId(int id);

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VOLUME_LEVEL, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_VOLUME_LEVEL)]
		void SetVolumeLevel(float volumeLevel);

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VOLUME_RAMP_UP, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_VOLUME_RAMP_UP)]
		void SetVolumeRampUp();

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VOLUME_RAMP_DOWN, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_VOLUME_RAMP_DOWN)]
		void SetVolumeRampDown();

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VOLUME_RAMP_STOP, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_VOLUME_RAMP_STOP)]
		void SetVolumeRampStop();

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VOLUME_MUTE, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_VOLUME_MUTE)]
		void SetVolumeMute(bool state);

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VOLUME_MUTE_TOGGLE, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_VOLUME_MUTE_TOGGLE)]
		void SetVolumeMuteToggle();
	}
}