using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Krang.SPlus.Routing.Endpoints.Destinations
{
	[KrangSettings("KrangAtHomeDestination", typeof(KrangAtHomeDestination))]
	public sealed class KrangAtHomeDestinationSettings : AbstractDestinationSettings
	{

		private const string AUDIO_OPTION_ELEMENT = "AudioOption";

		public eAudioOption AudioOption { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);
			writer.WriteElementString(AUDIO_OPTION_ELEMENT, AudioOption.ToString());
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			string audioOptionString = XmlUtils.TryReadChildElementContentAsString(xml, AUDIO_OPTION_ELEMENT);
			AudioOption =
				audioOptionString == null
					? eAudioOption.None
					: EnumUtils.Parse<eAudioOption>(audioOptionString, true);
		}
	}
}