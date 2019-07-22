using System;
using ICD.Connect.Devices.Proxies.Devices;
using ICD.Connect.Settings.Originators.Simpl;

namespace ICD.Connect.Krang.SPlus.SPlusRoomGroupControl.Proxy
{
	public sealed class ProxySPlusRoomGroupControl : AbstractProxyDevice<ProxySPlusRoomGroupControlSettings> , ISPlusRoomGroupControl
	{
		public void AllOff()
		{
			CallMethod(SPlusRoomGroupControlApi.METHOD_ALL_OFF);
		}

		public event EventHandler<RequestShimResyncEventArgs> OnRequestShimResync;
	}
}