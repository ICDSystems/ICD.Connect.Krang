using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Krang.Devices
{
	[KrangSettings("RemoteSwitcher", typeof(RemoteSwitcher))]
	public sealed class RemoteSwitcherSettings : AbstractDeviceSettings
	{
		private const string ADDRESS_ELEMENT = "Address";

		public HostInfo Address { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			if (Address != default(HostInfo))
				writer.WriteElementString(ADDRESS_ELEMENT, Address.ToString());
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			string address = XmlUtils.TryReadChildElementContentAsString(xml, ADDRESS_ELEMENT);
			if (string.IsNullOrEmpty(address))
				return;

			HostInfo info;
			if (HostInfo.TryParse(address, out info))
				Address = info;
		}
	}
}
