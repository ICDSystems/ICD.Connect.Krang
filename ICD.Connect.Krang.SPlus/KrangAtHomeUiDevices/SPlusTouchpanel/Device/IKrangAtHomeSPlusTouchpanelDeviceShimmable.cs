using System;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Device
{
	public interface IKrangAtHomeSPlusTouchpanelDeviceShimmable : IKrangAtHomeUiDeviceShimmable
	{
		event EventHandler<RoomListEventArgs> OnRoomListUpdate;

		event EventHandler<RoomSelectedEventArgs> OnRoomSelectedUpdate;

		event EventHandler<AudioSourceBaseListEventArgs> OnAudioSourceListUpdate;

		event EventHandler<VideoSourceBaseListEventArgs> OnVideoSourceListUpdate;

		event EventHandler<SourceSelectedEventArgs> OnSourceSelectedUpdate;

		event EventHandler<AudioSourceBaseListItemEventArgs> OnAudioSourceListItemUpdate;

		event EventHandler<VideoSourceBaseListItemEventArgs> OnVideoSourceListItemUpdate;

		event EventHandler<VolumeLevelFeedbackEventArgs> OnVolumeLevelFeedbackUpdate;

		event EventHandler<VolumeMuteFeedbackEventArgs> OnVolumeMuteFeedbackUpdate;

		event EventHandler<VolumeAvailableControlEventArgs> OnVolumeAvailableControlUpdate;

		void RequestDeviceRefresh();

		void SetRoomIndex(int index);

		void SetAudioSourceIndex(int index);

		void SetVideoSourceIndex(int index);

	}

}