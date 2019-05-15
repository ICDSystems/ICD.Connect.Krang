using System;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Krang.Core
{
	public sealed class LocalizationSettings
	{
		private const string ELEMENT_CULTURE = "Culture";
		private const string ELEMENT_UI_CULTURE = "UiCulture";

		#region Properties

		/// <summary>
		/// Gets/sets the culture name.
		/// </summary>
		public string Culture { get; set; }

		/// <summary>
		/// Gets/sets the UI culture name.
		/// </summary>
		public string UiCulture { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Clears the configured data.
		/// </summary>
		public void Clear()
		{
			Culture = null;
			UiCulture = null;
		}

		/// <summary>
		/// Copies the settings from the given other settings instance.
		/// </summary>
		/// <param name="settings"></param>
		public void Update(LocalizationSettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException("settings");

			Culture = settings.Culture;
			UiCulture = settings.UiCulture;
		}

		#endregion

		#region Serialization

		/// <summary>
		/// Updates the settings from the given xml element.
		/// </summary>
		/// <param name="xml"></param>
		public void ParseXml(string xml)
		{
			Culture = XmlUtils.TryReadChildElementContentAsString(xml, ELEMENT_CULTURE);
			UiCulture = XmlUtils.TryReadChildElementContentAsString(xml, ELEMENT_UI_CULTURE);
		}

		/// <summary>
		/// Writes the current configuration to the given XML writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="element"></param>
		public void ToXml(IcdXmlTextWriter writer, string element)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteStartElement(element);
			{
				writer.WriteElementString(ELEMENT_CULTURE, IcdXmlConvert.ToString(Culture));
				writer.WriteElementString(ELEMENT_UI_CULTURE, IcdXmlConvert.ToString(UiCulture));
			}
			writer.WriteEndElement();
		}

		#endregion
	}
}
