using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Devices;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Proxy;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Device
{
	[ApiClass(typeof(ProxySPlusTouchpanelDevice), typeof(IDevice))]
	public interface IKrangAtHomeSPlusTouchpanelDeviceShimmable : IKrangAtHomeUiDeviceShimmable
	{
		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_ROOM_LIST, SPlusTouchpanelDeviceApi.HELP_ROOM_LIST_EVENT)]
		event EventHandler<RoomListEventArgs> OnRoomListUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_ROOM_SELECTED, SPlusTouchpanelDeviceApi.HELP_ROOM_SELECTED_EVENT)]
		event EventHandler<RoomSelectedEventArgs> OnRoomSelectedUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_AUDIO_SOURCE_LIST, SPlusTouchpanelDeviceApi.HELP_AUDIO_SOURCE_LIST_EVENT)]
		event EventHandler<AudioSourceBaseListEventArgs> OnAudioSourceListUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_VIDEO_SOURCE_LIST, SPlusTouchpanelDeviceApi.HELP_VIDEO_SOURCE_LIST_EVENT)]
		event EventHandler<VideoSourceBaseListEventArgs> OnVideoSourceListUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_SOURCE_SELECTED, SPlusTouchpanelDeviceApi.HELP_SOURCE_INFO_EVENT)]
		event EventHandler<SourceSelectedEventArgs> OnSourceSelectedUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_AUDIO_SOURCE_LIST_ITEM, SPlusTouchpanelDeviceApi.HELP_AUDIO_SOURCE_LIST_ITEM)]
		event EventHandler<AudioSourceBaseListItemEventArgs> OnAudioSourceListItemUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_VIDEO_SOURCE_LIST_ITEM, SPlusTouchpanelDeviceApi.HELP_VIDEO_SOURCE_LIST_ITEM)]
		event EventHandler<VideoSourceBaseListItemEventArgs> OnVideoSourceListItemUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_VOLUME_LEVEL_FEEDBACK,
			SPlusTouchpanelDeviceApi.HELP_VOLUME_LEVEL_FEEDBACK_EVENT)]
		event EventHandler<VolumeLevelFeedbackEventArgs> OnVolumeLevelFeedbackUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_VOLUME_MUTE_FEEDBACK,
			SPlusTouchpanelDeviceApi.HELP_VOLUME_MUTE_FEEDBACK_EVENT)]
		event EventHandler<VolumeMuteFeedbackEventArgs> OnVolumeMuteFeedbackUpdate;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_VOLUME_AVAILABLE_CONTROL,
			SPlusTouchpanelDeviceApi.HELP_VOLUME_AVAILABLE_CONTROL_EVENT)]
		event EventHandler<VolumeAvailableControlEventArgs> OnVolumeAvailableControlUpdate;

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_REQUEST_REFRESH, SPlusTouchpanelDeviceApi.HELP_METHOD_REQUEST_REFRESH)]
		void RequestDeviceRefresh();

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_ROOM_INDEX, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_ROOM_INDEX)]
		void SetRoomIndex(int index);

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_AUDIO_SOURCE_INDEX,
			SPlusTouchpanelDeviceApi.HELP_METHOD_SET_AUDIO_SOURCE_INDEX)]
		void SetAudioSourceIndex(int index);

		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VIDEO_SOURCE_INDEX,
			SPlusTouchpanelDeviceApi.HELP_METHOD_SET_VIDEO_SOURCE_INDEX)]
		void SetVideoSourceIndex(int index);

	}

}