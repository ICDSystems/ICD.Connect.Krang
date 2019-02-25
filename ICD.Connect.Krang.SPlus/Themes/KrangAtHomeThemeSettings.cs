using System.Collections.Generic;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Themes;

namespace ICD.Connect.Krang.SPlus.Themes
{
	[KrangSettings("KrangAtHomeTheme", typeof(KrangAtHomeTheme))]
	public sealed class KrangAtHomeThemeSettings : AbstractThemeSettings
	{
		public const string ELEMENT_SYSTEM_ID = "SystemId";
		public const string ELEMENT_VIDEO_EQUIPMENT_ID = "VideoEquipmentId";
		public const string ELEMENT_AUDIO_EQUIPMENT_ID = "AudioEquipmentId";

		public const string ELEMENT_AUDIO_SOURCE_IDS = "AudioSourceIds";
		public const string ELEMENT_AUDIO_SOURCE_ID = "AudioSourceId";

		public const string ELEMENT_VIDEO_SOURCE_IDS = "VideoSourceIds";
		public const string ELEMENT_VIDEO_SOURCE_ID = "VideoSourceId";

		private readonly IcdHashSet<int> m_AudioSourceIds;
		private readonly IcdHashSet<int> m_VideoSourceIds;

		public int? SystemId { get; set; }

		public int? VideoEquipmentId { get; set; }

		public int? AudioEquipmentId { get; set; }

		public IcdHashSet<int> AudioSourceIds { get { return m_AudioSourceIds; } }

		public IcdHashSet<int> VideoSourceIds { get { return m_VideoSourceIds; } }

		public KrangAtHomeThemeSettings()
		{
			m_AudioSourceIds = new IcdHashSet<int>();
			m_VideoSourceIds = new IcdHashSet<int>();
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_SYSTEM_ID, IcdXmlConvert.ToString(SystemId));
			writer.WriteElementString(ELEMENT_AUDIO_EQUIPMENT_ID, IcdXmlConvert.ToString(AudioEquipmentId));
			writer.WriteElementString(ELEMENT_VIDEO_EQUIPMENT_ID, IcdXmlConvert.ToString(VideoEquipmentId));

			XmlUtils.WriteListToXml(writer, m_AudioSourceIds.Order(), ELEMENT_AUDIO_SOURCE_IDS, ELEMENT_AUDIO_SOURCE_ID);
			XmlUtils.WriteListToXml(writer, m_VideoSourceIds.Order(), ELEMENT_VIDEO_SOURCE_IDS, ELEMENT_VIDEO_SOURCE_ID);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			SystemId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_SYSTEM_ID);
			AudioEquipmentId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_AUDIO_EQUIPMENT_ID);
			VideoEquipmentId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_VIDEO_EQUIPMENT_ID);

			IEnumerable<int> audioSourceIds =
				XmlUtils.ReadListFromXml<int>(xml, ELEMENT_AUDIO_SOURCE_IDS, ELEMENT_AUDIO_SOURCE_ID);
			IEnumerable<int> videoSourceIds =
				XmlUtils.ReadListFromXml<int>(xml, ELEMENT_VIDEO_SOURCE_IDS, ELEMENT_VIDEO_SOURCE_ID);

			AudioSourceIds.Clear();
			AudioSourceIds.AddRange(audioSourceIds);

			VideoSourceIds.Clear();
			VideoSourceIds.AddRange(videoSourceIds);
		}
	}
}