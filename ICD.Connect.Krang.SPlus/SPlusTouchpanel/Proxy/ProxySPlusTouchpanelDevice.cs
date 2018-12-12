using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API;
using ICD.Connect.API.Info;
using ICD.Connect.Devices.Proxies.Devices;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.EventArgs;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.Proxy
{
	public sealed class ProxySPlusTouchpanelDevice : AbstractProxyDevice, IProxySPlusTouchpanelDevice
	{
		#region Events
		public event EventHandler<RoomListEventArgs> OnRoomListUpdate;
		public event EventHandler<RoomSelectedEventArgs> OnRoomSelectedUpdate;
		public event EventHandler<AudioSourceListEventArgs> OnAudioSourceListUpdate;
		public event EventHandler<VideoSourceListEventArgs> OnVideoSourceListUpdate;
		public event EventHandler<SourceSelectedEventArgs> OnSourceSelectedUpdate;
		public event EventHandler<VolumeLevelFeedbackEventArgs> OnVolumeLevelFeedbackUpdate;
		public event EventHandler<VolumeMuteFeedbackEventArgs> OnVolumeMuteFeedbackUpdate;
		public event EventHandler<VolumeAvailableControlEventArgs> OnVolumeAvailableControlUpdate;
		#endregion

		#region Methods
		public void RequestDeviceRefresh()
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_REQUEST_REFRESH);
		}

		public void SetRoomIndex(int index)
		{
			CallMethod(SPlusTouchpanelDeviceApi.HELP_METHOD_SET_ROOM_INDEX, index);
		}

		public void SetRoomId(int id)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_ROOM_ID, id );
		}

		public void SetAudioSourceIndex(int index)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_AUDIO_SOURCE_INDEX, index);
		}

		public void SetAudioSourceId(int id)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_AUDIO_SOURCE_ID, id);
		}

		public void SetVideoSourceIndex(int index)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VIDEO_SOURCE_INDEX, index);
		}

		public void SetVideoSourcdId(int id)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VIDEO_SOURCE_ID, id);
		}

		public void SetVolumeLevel(float volumeLevel)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VOLUME_LEVEL, volumeLevel);
		}

		public void SetVolumeRampUp()
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VOLUME_RAMP_UP);
		}

		public void SetVolumeRampDown()
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VOLUME_RAMP_DOWN);
		}

		public void SetVolumeRampStop()
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VOLUME_RAMP_STOP);
		}

		public void SetVolumeMute(bool state)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VOLUME_MUTE, state);
		}

		public void SetVolumeMuteToggle()
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VOLUME_MUTE_TOGGLE);
		}
		#endregion

		#region Private Methods

		private void RaiseRoomListUpdate(List<RoomInfo> roomList)
		{
			OnRoomListUpdate.Raise(this, new RoomListEventArgs(roomList));
		}

		private void RaiseRoomSelectedUpdate(RoomSelected roomSelected)
		{
			OnRoomSelectedUpdate.Raise(this, new RoomSelectedEventArgs(roomSelected));
		}

		private void RaiseAudioSourceListUpdate(List<SourceInfo> sourceList)
		{
			OnAudioSourceListUpdate.Raise(this, new AudioSourceListEventArgs(sourceList));
		}

		private void RaiseVideoSourceListUpdate(List<SourceInfo> sourceList)
		{
			OnVideoSourceListUpdate.Raise(this, new VideoSourceListEventArgs(sourceList));
		}

		private void RaiseSourceSelectedUpdate(SourceSelected sourceSelected)
		{
			OnSourceSelectedUpdate.Raise(this, new SourceSelectedEventArgs(sourceSelected));
		}

		private void RaiseVolumeLevelFeedbackUpdate(float volumeLevel)
		{
			OnVolumeLevelFeedbackUpdate.Raise(this, new VolumeLevelFeedbackEventArgs(volumeLevel));
		}

		private void RaiseVolumeMuteFeedbackUpdate(bool muteState)
		{
			OnVolumeMuteFeedbackUpdate.Raise(this, new VolumeMuteFeedbackEventArgs(muteState));
		}

		private void RaiseVolumeAvailableControlUpdate(VolumeAvailableControl availableControl)
		{
			OnVolumeAvailableControlUpdate.Raise(this, new VolumeAvailableControlEventArgs(availableControl));
		}


		#endregion

		#region API

		/// <summary>
		/// Override to build initialization commands on top of the current class info.
		/// </summary>
		/// <param name="command"></param>
		protected override void Initialize(ApiClassInfo command)
		{
			base.Initialize(command);

			ApiCommandBuilder.UpdateCommand(command)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_ROOM_LIST)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_ROOM_SELECTED)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_AUDIO_SOURCE_LIST)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_VIDEO_SOURCE_LIST)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_SOURCE_SELECTED)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_VOLUME_LEVEL_FEEDBACK)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_VOLUME_MUTE_FEEDBACK)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_VOLUME_MUTE_FEEDBACK)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_VOLUME_AVAILABLE_CONTROL)
							 .CallMethod(SPlusTouchpanelDeviceApi.METHOD_REQUEST_REFRESH)
							 .Complete();
		}

		/// <summary>
		/// Updates the proxy with event feedback info.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="result"></param>
		protected override void ParseEvent(string name, ApiResult result)
		{
			base.ParseEvent(name, result);

			switch (name)
			{
				case SPlusTouchpanelDeviceApi.EVENT_ROOM_LIST:
					RaiseRoomListUpdate(result.GetValue<List<RoomInfo>>());
					break;
				case SPlusTouchpanelDeviceApi.EVENT_ROOM_SELECTED:
					RaiseRoomSelectedUpdate(result.GetValue<RoomSelected>());
					break;
				case SPlusTouchpanelDeviceApi.EVENT_AUDIO_SOURCE_LIST:
					RaiseAudioSourceListUpdate(result.GetValue<List<SourceInfo>>());
					break;
				case SPlusTouchpanelDeviceApi.EVENT_VIDEO_SOURCE_LIST:
					RaiseVideoSourceListUpdate(result.GetValue<List<SourceInfo>>());
					break;
				case SPlusTouchpanelDeviceApi.EVENT_SOURCE_SELECTED:
					RaiseSourceSelectedUpdate(result.GetValue<SourceSelected>());
					break;
				case SPlusTouchpanelDeviceApi.EVENT_VOLUME_LEVEL_FEEDBACK:
					RaiseVolumeLevelFeedbackUpdate(result.GetValue<float>());
					break;
				case SPlusTouchpanelDeviceApi.EVENT_VOLUME_MUTE_FEEDBACK:
					RaiseVolumeMuteFeedbackUpdate(result.GetValue<bool>());
					break;
				case SPlusTouchpanelDeviceApi.EVENT_VOLUME_AVAILABLE_CONTROL:
					RaiseVolumeAvailableControlUpdate(result.GetValue<VolumeAvailableControl>());
					break;
			}
		}

		#endregion

		/// <summary>
		/// Gets/sets the device online status.
		/// </summary>
		public new bool IsOnline { get; set; }
	}
}