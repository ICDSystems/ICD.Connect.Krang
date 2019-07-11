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
	public sealed class ProxySPlusTouchpanelDevice : AbstractProxySPlusUiDevice<ProxySPlusTouchpanelDeviceSettings>, IKrangAtHomeSPlusTouchpanelDevice
	{
		#region Events

		public event EventHandler<RequestRefreshApiEventArgs> OnRequestRefresh;
		public event EventHandler<SetRoomIndexApiEventArgs> OnSetRoomIndex;

		public event EventHandler<SetAudioSourceIndexApiEventArgs> OnSetAudioSourceIndex;
		public event EventHandler<SetVideoSourceIndexApiEventArgs> OnSetVideoSourceIndex;

		#endregion

		#region Methods

		/// <summary>
		/// Sets the room info via the delegate
		/// </summary>
		/// <param name="roomInfo"></param>
		public void SetRoomInfo(RoomSelected roomInfo)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_ROOM_INFO, roomInfo);
		}

		/// <summary>
		/// Updates the room list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="roomList"></param>
		public void SetRoomList(List<RoomInfo> roomList)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_ROOM_LIST, roomList);
		}

		/// <summary>
		/// Sets the source info via the delegate
		/// </summary>
		/// <param name="sourceInfo"></param>
		public void SetSourceInfo(SourceSelected sourceInfo)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_SOURCE_INFO, sourceInfo);
		}

		/// <summary>
		/// Updates a single item on the audio list (for icon in use update, for example)
		/// </summary>
		/// <param name="sourceListItem"></param>
		public void SetAudioSourceListItem(SourceBaseListInfo sourceListItem)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_AUDIO_SOURCE_LIST_ITEM, sourceListItem);
		}

		/// <summary>
		/// Updates a single item on the video list (for icon in use update, for example)
		/// </summary>
		/// <param name="sourceListItem"></param>
		public void SetVideoSourceListItem(SourceBaseListInfo sourceListItem)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VIDEO_SOURCE_LIST_ITEM, sourceListItem);
		}

		/// <summary>
		/// Updates the audio source list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="sourceList"></param>
		public void SetAudioSourceList(List<SourceBaseListInfo> sourceList)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_AUDIO_SOURCE_LIST, sourceList);
		}

		/// <summary>
		/// Updates the video source list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="sourceList"></param>
		public void SetVideoSourceList(List<SourceBaseListInfo> sourceList)
		{
			CallMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VIDEO_SOURCE_LIST, sourceList);
		}

		#endregion

		#region Private Methods

		private void RaiseOnRequestRefresh()
		{
			OnRequestRefresh.Raise(this, new RequestRefreshApiEventArgs());
		}

		private void RaiseOnSetRoomIndex(int index)
		{
			OnSetRoomIndex.Raise(this, new SetRoomIndexApiEventArgs(index));
		}

		private void RaiseOnSetAudioSourceIndex(int index)
		{
			OnSetAudioSourceIndex.Raise(this, new SetAudioSourceIndexApiEventArgs(index));
		}

		private void RaiseOnSetVideoSourceIndex(int index)
		{
			OnSetVideoSourceIndex.Raise(this, new SetVideoSourceIndexApiEventArgs(index));
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
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_REQUEST_REFRESH)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_SET_ROOM_INDEX)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_SET_AUDIO_SOURCE_INDEX)
							 .SubscribeEvent(SPlusTouchpanelDeviceApi.EVENT_SET_VIDEO_SOURCE_INDEX)
							 .Complete();

			RaiseOnRequestRefresh();
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
				case SPlusTouchpanelDeviceApi.EVENT_REQUEST_REFRESH:
					RaiseOnRequestRefresh();
					break;
				case SPlusTouchpanelDeviceApi.EVENT_SET_ROOM_INDEX:
					RaiseOnSetRoomIndex(result.GetValue<int>());
					break;
				case SPlusTouchpanelDeviceApi.EVENT_SET_AUDIO_SOURCE_INDEX:
					RaiseOnSetAudioSourceIndex(result.GetValue<int>());
					break;
				case SPlusTouchpanelDeviceApi.EVENT_SET_VIDEO_SOURCE_INDEX:
					RaiseOnSetVideoSourceIndex(result.GetValue<int>());
					break;
			}
		}

		#endregion
	}
}