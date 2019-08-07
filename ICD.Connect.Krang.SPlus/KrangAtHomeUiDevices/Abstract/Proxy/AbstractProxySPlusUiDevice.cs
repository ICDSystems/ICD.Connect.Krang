using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API;
using ICD.Connect.API.Info;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Proxies.Devices;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.EventArgs;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Proxy
{
	public abstract class AbstractProxySPlusUiDevice<TSettings> : AbstractProxyDevice<TSettings>, IKrangAtHomeUiDevice
		where TSettings : IProxyDeviceSettings
	{

		#region Events
		public event EventHandler<RequestRefreshApiEventArgs> OnRequestRefresh;
		public event EventHandler<SetRoomIdApiEventArgs> OnSetRoomId;
		public event EventHandler<SetAudioSourceIdApiEventArgs> OnSetAudioSourceId;
		public event EventHandler<SetVideoSourceIdApiEventArgs> OnSetVideoSourceId;
		#endregion


		#region Methods
		
		public void SetVolumeControl(DeviceControlInfo controlInfo)
		{
			CallMethod(SPlusUiDeviceApi.METHOD_SET_VOLUME_CONTROL, controlInfo);
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
			                 .SubscribeEvent(SPlusUiDeviceApi.EVENT_SET_ROOM_ID)
			                 .SubscribeEvent(SPlusUiDeviceApi.EVENT_SET_AUDIO_SOURCE_ID)
			                 .SubscribeEvent(SPlusUiDeviceApi.EVENT_SET_VIDEO_SOURCE_ID)
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
				case SPlusUiDeviceApi.EVENT_REQUEST_REFRESH:
					RaiseOnRequestRefresh();
					break;
				case SPlusUiDeviceApi.EVENT_SET_ROOM_ID:
					RaiseSetRoomId(result.GetValue<int>());
					break;
				case SPlusUiDeviceApi.EVENT_SET_AUDIO_SOURCE_ID:
					RaiseSetAudioSourceId(result.GetValue<int>());
					break;
				case SPlusUiDeviceApi.EVENT_SET_VIDEO_SOURCE_ID:
					RaiseSetVideoSourceId(result.GetValue<int>());
					break;
			}
		}

		#endregion

		#region Raise Events

		protected void RaiseOnRequestRefresh()
		{
			OnRequestRefresh.Raise(this, new RequestRefreshApiEventArgs());
		}

		private void RaiseSetRoomId(int data)
		{
			OnSetRoomId.Raise(this, new SetRoomIdApiEventArgs(data));
		}

		private void RaiseSetAudioSourceId(int data)
		{
			OnSetAudioSourceId.Raise(this, new SetAudioSourceIdApiEventArgs(data));
		}

		private void RaiseSetVideoSourceId(int data)
		{
			OnSetVideoSourceId.Raise(this, new SetVideoSourceIdApiEventArgs(data));
		}

		#endregion

	}
}