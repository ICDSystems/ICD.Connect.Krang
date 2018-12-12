using ICD.Connect.Devices.Simpl;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.Device
{
	[KrangSettings("KrangAtHomeSPlusPanel", typeof(KrangAtHomeSPlusTouchpanelDevice))]
	public sealed class KrangAtHomeSPlusTouchpanelDeviceSettings : AbstractSimplDeviceSettings
	{
	}
}