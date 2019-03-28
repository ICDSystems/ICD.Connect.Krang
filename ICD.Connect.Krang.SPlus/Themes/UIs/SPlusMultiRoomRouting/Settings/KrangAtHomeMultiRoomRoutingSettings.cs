using System.Collections.Generic;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Routing.Connections;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Settings
{
	public sealed class KrangAtHomeMultiRoomRoutingSettings
	{
		private const string ELEMENT_MULTI_ROOM_ROUTING = "MultiRoomRouting";
		private const string ATTRIBUTE_EQUIPMENT_ID = "equipmentId";

		private const string ELEMENT_NAME = "Name";

		private const string ELEMENT_CONNECTION_TYPE = "ConnectionType";

		private const string ELEMENT_SOURCE_IDS = "SourceIds";
		private const string ELEMENT_SOURCE_ID = "SourceId";

		private const string ELEMENT_ROOM_GROUP_IDS = "RoomGroupIds";
		private const string ELEMENT_ROOM_GROUP_ID = "RoomGroupId";
        private const string ATTRIBUTE_ROOM_GROUP_INDEX = "index";

		private readonly IcdHashSet<int> m_SourceIds;

		/// <summary>
		/// Room ID's - key = index, value = originatorId
		/// </summary>
		private readonly Dictionary<int, int> m_RoomGroupIds;

		public int EquipmentId { get; set; }

		public string Name { get; set; }

		public eConnectionType ConnectionType { get; set; }

		public IcdHashSet<int> SourceIds { get { return m_SourceIds; } }

		public Dictionary<int, int> RoomGroupIds { get { return m_RoomGroupIds; } }

		public KrangAtHomeMultiRoomRoutingSettings()
		{
			m_RoomGroupIds = new Dictionary<int, int>();
			m_SourceIds = new IcdHashSet<int>();
		}

		public static KeyValuePair<int,KrangAtHomeMultiRoomRoutingSettings> ReadMultiRoomRoutingFromXml(string xml)
		{
			KrangAtHomeMultiRoomRoutingSettings settings = new KrangAtHomeMultiRoomRoutingSettings();

			settings.EquipmentId = XmlUtils.GetAttributeAsInt(xml, ATTRIBUTE_EQUIPMENT_ID);

			settings.Name = XmlUtils.ReadChildElementContentAsString(xml, ELEMENT_NAME);

			settings.ConnectionType = XmlUtils.ReadChildElementContentAsEnum<eConnectionType>(xml,ELEMENT_CONNECTION_TYPE, true);

			IEnumerable<int> sourceIds =
				XmlUtils.ReadListFromXml<int>(xml, ELEMENT_SOURCE_IDS, ELEMENT_SOURCE_ID);

			settings.SourceIds.Clear();
			settings.SourceIds.AddRange(sourceIds);

			IEnumerable<KeyValuePair<int, int>> roomIds =
				XmlUtils.ReadListFromXml<KeyValuePair<int, int>>(xml, ELEMENT_ROOM_GROUP_IDS, ELEMENT_ROOM_GROUP_ID,
																 ReadRoomGroupFromXml);

			settings.RoomGroupIds.Clear();
			settings.RoomGroupIds.AddRange(roomIds);
			
			return new KeyValuePair<int, KrangAtHomeMultiRoomRoutingSettings>(settings.EquipmentId, settings);
		}

		public static void WriteMultiRoomRoutingToXml(IcdXmlTextWriter writer, KrangAtHomeMultiRoomRoutingSettings settings)
		{
			writer.WriteStartElement(ELEMENT_MULTI_ROOM_ROUTING);
			{
				writer.WriteAttributeString(ATTRIBUTE_EQUIPMENT_ID, IcdXmlConvert.ToString(settings.EquipmentId));
				writer.WriteElementString(ELEMENT_NAME, IcdXmlConvert.ToString(settings.Name));
				writer.WriteElementString(ELEMENT_CONNECTION_TYPE, IcdXmlConvert.ToString(settings.ConnectionType));
				XmlUtils.WriteListToXml(writer, settings.SourceIds.Order(), ELEMENT_SOURCE_IDS, ELEMENT_SOURCE_ID);
				XmlUtils.WriteListToXml(writer, settings.RoomGroupIds, ELEMENT_ROOM_GROUP_IDS, WriteRoomGroupToXml);
			}
		}


		private static KeyValuePair<int, int> ReadRoomGroupFromXml(string xml)
		{
			int index = XmlUtils.GetAttributeAsInt(xml, ATTRIBUTE_ROOM_GROUP_INDEX);
			int id = XmlUtils.ReadElementContentAsInt(xml);

			return new KeyValuePair<int, int>(index, id);
		}

		private static void WriteRoomGroupToXml(IcdXmlTextWriter writer, KeyValuePair<int, int> kvp)
		{
			writer.WriteStartElement(ELEMENT_ROOM_GROUP_ID);
			{
				writer.WriteAttributeString(ATTRIBUTE_ROOM_GROUP_INDEX, IcdXmlConvert.ToString(kvp.Key));
				writer.WriteString(IcdXmlConvert.ToString(kvp.Value));
			}
			writer.WriteEndElement();
		}
	}
}