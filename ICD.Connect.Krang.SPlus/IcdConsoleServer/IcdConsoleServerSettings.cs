using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Krang.SPlus.IcdConsoleServer
{
	[KrangSettings("IcdConsoleServer", typeof(IcdConsoleServerDevice))]
	public sealed class IcdConsoleServerSettings : AbstractDeviceSettings
	{

		private const string PORT_NUMBER_ELEMENT = "PortNumber";
		private const ushort DEFAULT_PORT_NUMBER = 8023;

		public ushort PortNumber { get; set; }

		public ushort DefaultPortNumber { get { return DEFAULT_PORT_NUMBER; }}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);
			PortNumber = XmlUtils.TryReadChildElementContentAsUShort(xml, PORT_NUMBER_ELEMENT) ?? DEFAULT_PORT_NUMBER;
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);
			writer.WriteElementString(PORT_NUMBER_ELEMENT, IcdXmlConvert.ToString(PortNumber));
		}
	}
}