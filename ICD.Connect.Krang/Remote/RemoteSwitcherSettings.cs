using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Krang.Remote
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class RemoteSwitcherSettings : AbstractDeviceSettings
	{
		private const string FACTORY_NAME = "RemoteSwitcher";
		private const string ADDRESS_ELEMENT = "Address";

		#region Properties

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(RemoteSwitcher); } }

		public HostInfo Address { get; set; }

		#endregion

		#region Methods

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

		#endregion
	}
}
