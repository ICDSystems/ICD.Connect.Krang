namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Proxy
{
	public static class SPlusTouchpanelDeviceApi
	{
		public const string EVENT_ROOM_LIST = "OnRoomListUpdate";
		public const string EVENT_ROOM_SELECTED = "OnRoomSelectedUpdate";
		public const string EVENT_AUDIO_SOURCE_LIST = "OnAudioSourceListUpdate";
		public const string EVENT_VIDEO_SOURCE_LIST = "OnVideoSourceListUpdate";
		public const string EVENT_SOURCE_SELECTED = "OnSourceSelectedUpdate";
		public const string EVENT_VOLUME_LEVEL_FEEDBACK = "OnVolumeLevelFeedbackUpdate";
		public const string EVENT_VOLUME_MUTE_FEEDBACK = "OnVolumeMuteFeedbackUpdate";
		public const string EVENT_VOLUME_AVAILABLE_CONTROL = "OnVolumeAvailableControlUpdate";

		public const string METHOD_REQUEST_REFRESH = "RequestDeviceRefresh";
		public const string METHOD_SET_ROOM_INDEX = "SetRoomIndex";
		
		public const string METHOD_SET_AUDIO_SOURCE_INDEX = "SetAudioSourceIndex";
		
		public const string METHOD_SET_VIDEO_SOURCE_INDEX = "SetVideoSourceIndex";
		
		


		public const string HELP_ROOM_LIST_EVENT = "Raised when the room list needs to be updated";
		public const string HELP_ROOM_SELECTED_EVENT = "Raised when the selected room info needs to be updated";
		public const string HELP_AUDIO_SOURCE_LIST_EVENT = "Raised when the audio source list needs to be updated";
		public const string HELP_VIDEO_SOURCE_LIST_EVENT = "Raised when the video source list needs to be updated";
		public const string HELP_SOURCE_INFO_EVENT = "Raised when the selected source info needs to be updated";
		public const string HELP_VOLUME_LEVEL_FEEDBACK_EVENT = "Raised when the volume level feedback needs to be updated";
		public const string HELP_VOLUME_MUTE_FEEDBACK_EVENT = "Raised when the volume mute feedback needs to be updated";
		public const string HELP_VOLUME_AVAILABLE_CONTROL_EVENT =
			"Raised when the available volume controls need to be updated";

		public const string HELP_METHOD_REQUEST_REFRESH = "";
		public const string HELP_METHOD_SET_ROOM_INDEX = "";
		
		public const string HELP_METHOD_SET_AUDIO_SOURCE_INDEX = "";
		
		public const string HELP_METHOD_SET_VIDEO_SOURCE_INDEX = "";
		
		
	}
}