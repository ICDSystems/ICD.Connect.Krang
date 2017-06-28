using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;

namespace ICD.Connect.Krang.Remote
{
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
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlDeviceSettingsFactoryMethod(FACTORY_NAME)]
		public static RemoteSwitcherSettings FromXml(string xml)
		{
			RemoteSwitcherSettings output = new RemoteSwitcherSettings();

			ParseXml(output, xml);
			return output;
		}

		private static void ParseXml(RemoteSwitcherSettings output, string xml)
		{
			string address = XmlUtils.TryReadChildElementContentAsString(xml, ADDRESS_ELEMENT);
			if (!string.IsNullOrEmpty(address))
			{
				HostInfo info;
				if (HostInfo.TryParse(address, out info))
					output.Address = info;
			}
			AbstractDeviceSettings.ParseXml(output, xml);
		}

		#endregion
	}
}
