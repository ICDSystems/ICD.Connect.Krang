using ICD.Connect.Devices.Proxies.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.Proxy
{
	[KrangSettings("ProxyKrangAtHomeSPlusPanel", typeof(ProxySPlusTouchpanelDevice))]
	public sealed class ProxySPlusTouchpanelDeviceSettings : AbstractProxyDeviceSettings
	{
	}
}