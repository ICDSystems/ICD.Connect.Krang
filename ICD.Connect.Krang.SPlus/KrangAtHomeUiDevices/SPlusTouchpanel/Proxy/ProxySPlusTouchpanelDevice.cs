using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API;
using ICD.Connect.API.Info;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Proxy;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Proxy
{
	public sealed class ProxySPlusTouchpanelDevice : AbstractProxySPlusUiDevice<ProxySPlusTouchpanelDeviceSettings>, IKrangAtHomeSPlusTouchpanelDeviceShimmable
	{
		#region Events
		public event EventHandler<RoomListEventArgs> OnRoomListUpdate;
		public event EventHandler<RoomSelectedEventArgs> OnRoomSelectedUpdate;
		public event EventHandler<AudioSourceBaseListEventArgs> OnAudioSourceListUpdate;
		public event EventHandler<VideoSourceBaseListEventArgs> OnVideoSourceListUpdate;
		public event EventHandler<SourceSelectedEventArgs> OnSourceSelectedUpdate;
		public event EventHandler<AudioSourceBaseListItemEventArgs> OnAudioSourceListItemUpdate;
		public event EventHandler<VideoSourceBaseListItemEventArgs> OnVideoSourceListItemUpdate;
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
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_ROOM_INDEX, index);
		}

		public void SetAudioSourceIndex(int index)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_AUDIO_SOURCE_INDEX, index);
		}

		public void SetVideoSourceIndex(int index)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VIDEO_SOURCE_INDEX, index);
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

		private void RaiseAudioSourceListUpdate(List<SourceBaseListInfo> sourceList)
		{
			OnAudioSourceListUpdate.Raise(this, new AudioSourceBaseListEventArgs(sourceList));
		}

		private void RaiseVideoSourceListUpdate(List<SourceBaseListInfo> sourceList)
		{
			OnVideoSourceListUpdate.Raise(this, new VideoSourceBaseListEventArgs(sourceList));
		}

		private void RaiseSourceSelectedUpdate(SourceSelected sourceSelected)
		{
			OnSourceSelectedUpdate.Raise(this, new SourceSelectedEventArgs(sourceSelected));
		}

		private void RaiseAudioSourceListItemUpdate(SourceBaseListInfo sourceListItem)
		{
			OnAudioSourceListItemUpdate.Raise(this, new AudioSourceBaseListItemEventArgs(sourceListItem));
		}

		private void RaiseVideoSourceListItemUpdate(SourceBaseListInfo sourceListItem)
		{
			OnVideoSourceListItemUpdate.Raise(this, new VideoSourceBaseListItemEventArgs(sourceListItem));
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
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_AUDIO_SOURCE_LIST_ITEM)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_VIDEO_SOURCE_LIST_ITEM)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_VOLUME_LEVEL_FEEDBACK)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_VOLUME_MUTE_FEEDBACK)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_VOLUME_AVAILABLE_CONTROL)
							 .CallMethod(SPlusTouchpanelDeviceApi.METHOD_REQUEST_REFRESH)
							 .Complete();
			RaiseOnRequestShimResync(this);
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
					RaiseAudioSourceListUpdate(result.GetValue<List<SourceBaseListInfo>>());
					break;
				case SPlusTouchpanelDeviceApi.EVENT_VIDEO_SOURCE_LIST:
					RaiseVideoSourceListUpdate(result.GetValue<List<SourceBaseListInfo>>());
					break;
				case SPlusTouchpanelDeviceApi.EVENT_SOURCE_SELECTED:
					RaiseSourceSelectedUpdate(result.GetValue<SourceSelected>());
					break;
				case SPlusTouchpanelDeviceApi.EVENT_AUDIO_SOURCE_LIST_ITEM:
					RaiseAudioSourceListItemUpdate(result.GetValue<SourceBaseListInfo>());
					break;
				case SPlusTouchpanelDeviceApi.EVENT_VIDEO_SOURCE_LIST_ITEM:
					RaiseVideoSourceListItemUpdate(result.GetValue<SourceBaseListInfo>());
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
	}
}