using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Routing.Endpoints.Sources
{
	public sealed class SimplSourceSettings : AbstractSourceSettings
	{
		private const string FACTORY_NAME = "SimplSource";

		private const string CROSSPOINT_ID_ELEMENT = "CrosspointId";
		private const string CROSSPOINT_TYPE_ELEMENT = "CrosspointType";
		private const string SOURCE_VISIBILITY_ELEMENT = "SourceVisibility";

		#region Properties

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		[PublicAPI]
		public ushort CrosspointId { get; set; }

		public ushort CrosspointType { get; set; }

		[PublicAPI]
		public SimplSource.eSourceVisibility SourceVisibility { get; set; }

		#endregion

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(CROSSPOINT_ID_ELEMENT, IcdXmlConvert.ToString(CrosspointId));
			writer.WriteElementString(CROSSPOINT_TYPE_ELEMENT, IcdXmlConvert.ToString(CrosspointType));
			writer.WriteElementString(SOURCE_VISIBILITY_ELEMENT, SourceVisibility.ToString());
		}

		/// <summary>
		/// Creates a new originator instance from the settings.
		/// </summary>
		/// <param name="factory"></param>
		/// <returns></returns>
		public override IOriginator ToOriginator(IDeviceFactory factory)
		{
			SimplSource output = new SimplSource();
			output.ApplySettings(this, factory);
			return output;
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlSourceSettingsFactoryMethod(FACTORY_NAME)]
		public static SimplSourceSettings FromXml(string xml)
		{
			SimplSourceSettings output = new SimplSourceSettings
			{
				CrosspointId = XmlUtils.TryReadChildElementContentAsUShort(xml, CROSSPOINT_ID_ELEMENT) ?? 0,
				CrosspointType = XmlUtils.TryReadChildElementContentAsUShort(xml, CROSSPOINT_TYPE_ELEMENT) ?? 0,
				SourceVisibility =
					XmlUtils.TryReadChildElementContentAsEnum<SimplSource.eSourceVisibility>(xml, SOURCE_VISIBILITY_ELEMENT, true) ??
					SimplSource.eSourceVisibility.None
			};

			ParseXml(output, xml);
			return output;
		}
	}
}
