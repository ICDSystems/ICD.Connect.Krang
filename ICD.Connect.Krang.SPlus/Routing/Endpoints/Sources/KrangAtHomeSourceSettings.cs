using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Simpl;

namespace ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources
{
	[KrangSettings("KrangAtHomeSource", typeof(KrangAtHomeSource))]
	public sealed class KrangAtHomeSourceSettings : AbstractSourceSettings, ISimplOriginatorSettings
	{
		private const string CROSSPOINT_ID_ELEMENT = "CrosspointId";
		private const string CROSSPOINT_TYPE_ELEMENT = "CrosspointType";
		private const string SOURCE_VISIBILITY_ELEMENT = "SourceVisibility";

		#region Properties

		[PublicAPI]
		public ushort CrosspointId { get; set; }

		public ushort CrosspointType { get; set; }

		[PublicAPI]
		public KrangAtHomeSource.eSourceVisibility SourceVisibility { get; set; }

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
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			CrosspointId = XmlUtils.TryReadChildElementContentAsUShort(xml, CROSSPOINT_ID_ELEMENT) ?? 0;
			CrosspointType = XmlUtils.TryReadChildElementContentAsUShort(xml, CROSSPOINT_TYPE_ELEMENT) ?? 0;
			SourceVisibility =
				XmlUtils.TryReadChildElementContentAsEnum<KrangAtHomeSource.eSourceVisibility>(xml, SOURCE_VISIBILITY_ELEMENT, true) ??
				KrangAtHomeSource.eSourceVisibility.None;
		}
	}
}
