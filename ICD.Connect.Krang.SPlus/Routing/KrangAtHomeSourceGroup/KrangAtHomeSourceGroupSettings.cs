using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup
{
	[KrangSettings("KrangAtHomeSourceGroup", typeof(KrangAtHomeSourceGroup))]
	public sealed class KrangAtHomeSourceGroupSettings : AbstractDeviceSettings
	{
		#region Constants
		private const string SOURCE_VISIBILITY_ELEMENT = "SourceVisibility";
		private const string ORDER_ELEMENT = "Order";
		private const string LIST_ELEMENT = "Sources";
		private const string LIST_CHILD_ELEMENT = "Source";
		private const string PRIORITY_ATTRIBUTE = "priority";

		#endregion

		#region Properties

		/// <summary>
		/// Sources
		/// Key is SourceId
		/// Value is Priority
		/// </summary>
		public Dictionary<int, int> Sources { get; set; }

		/// <summary>
		/// What lists this source group should show up under
		/// </summary>
		public eSourceVisibility SourceVisibility { get; set; }

		public int Order { get; set; }

		#endregion

		#region Constructor

		public KrangAtHomeSourceGroupSettings()
		{
			Sources = new Dictionary<int, int>();
		}

		#endregion

		#region XML

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			SourceVisibility =
				XmlUtils.TryReadChildElementContentAsEnum<eSourceVisibility>(xml, SOURCE_VISIBILITY_ELEMENT, true) ??
				eSourceVisibility.None;

			Order = XmlUtils.TryReadChildElementContentAsInt(xml, ORDER_ELEMENT) ?? int.MaxValue;

			Sources.AddRange(XmlUtils.ReadListFromXml<KeyValuePair<int, int>>(xml, LIST_ELEMENT, LIST_CHILD_ELEMENT, ReadChildFromXml));
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(SOURCE_VISIBILITY_ELEMENT, SourceVisibility.ToString());

			writer.WriteElementString(ORDER_ELEMENT, IcdXmlConvert.ToString(Order));

			XmlUtils.WriteListToXml(writer, Sources, LIST_ELEMENT, WriteChildToXml);
		}

		#endregion

		#region Static Methods

        /// <summary>
        /// Reads a single source/priority pair from the XML
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
		private static KeyValuePair<int, int> ReadChildFromXml(string xml)
		{
			int? attribute =
				XmlUtils.HasAttribute(xml, PRIORITY_ATTRIBUTE)
					? (int?)XmlUtils.GetAttributeAsInt(xml, PRIORITY_ATTRIBUTE)
					: null;

			int priority =
				attribute.HasValue
					? attribute.Value
					: int.MaxValue;

			int id = XmlUtils.ReadElementContentAsInt(xml);

			return new KeyValuePair<int, int>(id, priority);
		}

		/// <summary>
		/// Writes a single source/priority pair to the xml
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="kvp"></param>
		private static void WriteChildToXml(IcdXmlTextWriter writer, KeyValuePair<int, int> kvp)
		{
			writer.WriteStartElement(LIST_CHILD_ELEMENT);
			{
				writer.WriteAttributeString(PRIORITY_ATTRIBUTE, kvp.Value.ToString());
				writer.WriteString(IcdXmlConvert.ToString(kvp.Key));
			}
			writer.WriteEndElement();
		}

		#endregion

	}
}