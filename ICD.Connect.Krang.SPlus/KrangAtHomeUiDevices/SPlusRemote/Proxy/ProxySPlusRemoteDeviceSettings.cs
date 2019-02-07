using ICD.Connect.Devices.Proxies.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Proxy
{
	[KrangSettings("ProxyKrangAtHomeSPlusRemote", typeof(ProxySPlusRemoteDevice))]
	public sealed class ProxySPlusRemoteDeviceSettings : AbstractProxyDeviceSettings
	{
	}
}