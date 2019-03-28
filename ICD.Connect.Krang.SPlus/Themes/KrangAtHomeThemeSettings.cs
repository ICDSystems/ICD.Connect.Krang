using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Settings;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Themes;

namespace ICD.Connect.Krang.SPlus.Themes
{
	[KrangSettings("KrangAtHomeTheme", typeof(KrangAtHomeTheme))]
	public sealed class KrangAtHomeThemeSettings : AbstractThemeSettings
	{
		public const string ELEMENT_SYSTEM_ID = "SystemId";
		private const string ELEMENT_MULTI_ROOM_ROUTINGS = "MultiRoomRoutings";
		private const string ELEMENT_MULTI_ROOM_ROUTING = "MultiRoomRouting";

		private readonly Dictionary<int, KrangAtHomeMultiRoomRoutingSettings> m_MultiRoomRoutings;

		public Dictionary<int,KrangAtHomeMultiRoomRoutingSettings> MultiRoomRoutings { get { return m_MultiRoomRoutings; }}

		public int? SystemId { get; set; }

		public KrangAtHomeThemeSettings()
		{
			m_MultiRoomRoutings = new Dictionary<int, KrangAtHomeMultiRoomRoutingSettings>();
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_SYSTEM_ID, IcdXmlConvert.ToString(SystemId));

			XmlUtils.WriteListToXml(writer, MultiRoomRoutings.Values, ELEMENT_MULTI_ROOM_ROUTINGS, KrangAtHomeMultiRoomRoutingSettings.WriteMultiRoomRoutingToXml);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			SystemId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_SYSTEM_ID);

			IEnumerable<KeyValuePair<int, KrangAtHomeMultiRoomRoutingSettings>> multiRoomRoutings =
				XmlUtils.ReadListFromXml<KeyValuePair<int, KrangAtHomeMultiRoomRoutingSettings>>(xml, ELEMENT_MULTI_ROOM_ROUTINGS,
				                                                                                 ELEMENT_MULTI_ROOM_ROUTING,
				                                                                                 KrangAtHomeMultiRoomRoutingSettings
					                                                                                 .ReadMultiRoomRoutingFromXml);
			MultiRoomRoutings.Clear();
			MultiRoomRoutings.AddRange(multiRoomRoutings);
		}
	}
}