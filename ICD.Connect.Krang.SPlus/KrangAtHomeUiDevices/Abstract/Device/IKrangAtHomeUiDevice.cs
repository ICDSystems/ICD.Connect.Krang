using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.EventArgs;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Proxy;
using ICD.Connect.Settings.Originators;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device
{
	public interface IKrangAtHomeUiDevice : IOriginator
	{

		#region Events

		[ApiEvent(SPlusUiDeviceApi.EVENT_SET_ROOM_ID, SPlusUiDeviceApi.HELP_EVENT_SET_ROOM_ID)]
		event EventHandler<SetRoomIdApiEventArgs> OnSetRoomId;

		[ApiEvent(SPlusUiDeviceApi.EVENT_SET_AUDIO_SOURCE_ID, SPlusUiDeviceApi.HELP_EVENT_SET_AUDIO_SOURCE_ID)]
		event EventHandler<SetAudioSourceIdApiEventArgs> OnSetAudioSourceId;

		[ApiEvent(SPlusUiDeviceApi.EVENT_SET_VIDEO_SOURCE_ID, SPlusUiDeviceApi.HELP_EVENT_SET_VIDEO_SOURCE_ID)]
		event EventHandler<SetVideoSourceIdApiEventArgs> OnSetVideoSourceId;

		#endregion

		#region Methods

		[ApiMethod(SPlusUiDeviceApi.METHOD_SET_VOLUME_CONTROL, SPlusUiDeviceApi.HELP_METHOD_SET_VOLUME_CONTROL)]
		void SetVolumeControl(DeviceControlInfo controlInfo);

		#endregion



	}
}