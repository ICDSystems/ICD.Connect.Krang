using System;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API;
using ICD.Connect.API.Info;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Proxy;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.EventArgs;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Proxy
{
	public sealed class ProxySPlusRemoteDevice : AbstractProxySPlusUiDevice<ProxySPlusRemoteDeviceSettings>, IKrangAtHomeSPlusRemoteDeviceShimmable
	{
		public event EventHandler<RoomChangedApiEventArgs> OnRoomChanged;

		public event EventHandler<SourceChangedApiEventArgs> OnSourceChanged;

		/// <summary>
		/// Override to build initialization commands on top of the current class info.
		/// </summary>
		/// <param name="command"></param>
		protected override void Initialize(ApiClassInfo command)
		{
			base.Initialize(command);

			ApiCommandBuilder.UpdateCommand(command)
							 .SubscribeEvent(SPlusRemoteApi.EVENT_ROOM_CHANGED)
							 .SubscribeEvent(SPlusRemoteApi.EVENT_SOURCE_CHANGED)
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
				case SPlusRemoteApi.EVENT_ROOM_CHANGED:
					RaiseRoomChanged(result.GetValue<RoomInfo>());
					break;
				case SPlusRemoteApi.EVENT_SOURCE_CHANGED:
					RaiseSourceChanged(result.GetValue<SourceInfo>());
					break;
			}
		}


		private void RaiseRoomChanged(RoomInfo roomInfo)
		{
			OnRoomChanged.Raise(this, new RoomChangedApiEventArgs(roomInfo));
		}

		private void RaiseSourceChanged(SourceInfo sourceInfo)
		{
			OnSourceChanged.Raise(this, new SourceChangedApiEventArgs(sourceInfo));
		}

	}
}