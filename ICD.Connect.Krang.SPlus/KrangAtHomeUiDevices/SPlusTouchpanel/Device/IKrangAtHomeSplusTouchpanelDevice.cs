using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Attributes;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Proxy;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Device
{
	public interface IKrangAtHomeSPlusTouchpanelDevice: IKrangAtHomeUiDevice
	{

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_REQUEST_REFRESH, SPlusTouchpanelDeviceApi.HELP_EVENT_REQUEST_REFRESH)]
		event EventHandler<RequestRefreshApiEventArgs> OnRequestRefresh;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_SET_ROOM_INDEX, SPlusTouchpanelDeviceApi.HELP_EVENT_SET_ROOM_INDEX)]
		event EventHandler<SetRoomIndexApiEventArgs> OnSetRoomIndex;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_SET_AUDIO_SOURCE_INDEX, SPlusTouchpanelDeviceApi.HELP_EVENT_SET_AUDIO_SOURCE_INDEX)]
		event EventHandler<SetAudioSourceIndexApiEventArgs> OnSetAudioSourceIndex;

		[ApiEvent(SPlusTouchpanelDeviceApi.EVENT_SET_VIDEO_SOURCE_INDEX, SPlusTouchpanelDeviceApi.HELP_EVENT_SET_VIDEO_SOURCE_INDEX)]
		event EventHandler<SetVideoSourceIndexApiEventArgs> OnSetVideoSourceIndex;

		/// <summary>
		/// Sets the room info via the delegate
		/// </summary>
		/// <param name="roomInfo"></param>
		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_ROOM_INFO,SPlusTouchpanelDeviceApi.HELP_METHOD_SET_ROOM_INFO)]
		void SetRoomInfo(RoomSelected roomInfo);


		/// <summary>
		/// Updates the room list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="roomList"></param>
		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_ROOM_LIST, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_ROOM_LIST)]
		void SetRoomList(List<RoomInfo> roomList);

		/// <summary>
		/// Sets the source info via the delegate
		/// </summary>
		/// <param name="sourceInfo"></param>
		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_SOURCE_INFO, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_SOURCE_INFO)]
		void SetSourceInfo(SourceSelected sourceInfo);


		/// <summary>
		/// Updates a single item on the audio list (for icon in use update, for example)
		/// </summary>
		/// <param name="sourceListItem"></param>
		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_AUDIO_SOURCE_LIST_ITEM, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_AUDIO_SOURCE_LIST_ITEM)]
		void SetAudioSourceListItem(SourceBaseListInfo sourceListItem);


		/// <summary>
		/// Updates a single item on the video list (for icon in use update, for example)
		/// </summary>
		/// <param name="sourceListItem"></param>
		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VIDEO_SOURCE_LIST_ITEM, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_VIDEO_SOURCE_LIST_ITEM)]
		void SetVideoSourceListItem(SourceBaseListInfo sourceListItem);

		/// <summary>
		/// Updates the audio source list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="sourceList"></param>
		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_AUDIO_SOURCE_LIST, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_AUDIO_SOURCE_LIST)]
		void SetAudioSourceList(List<SourceBaseListInfo> sourceList);

		/// <summary>
		/// Updates the video source list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="sourceList"></param>
		[ApiMethod(SPlusTouchpanelDeviceApi.METHOD_SET_VIDEO_SOURCE_LIST, SPlusTouchpanelDeviceApi.HELP_METHOD_SET_VIDEO_SOURCE_LIST)]
		void SetVideoSourceList(List<SourceBaseListInfo> sourceList);
	}
}