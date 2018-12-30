using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.VolumePoints;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Destinations;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Krang.SPlus.VolumePoints
{
	[KrangSettings(FACTORY_NAME, typeof(KrangAtHomeVolumePoint))]
	public sealed class KrangAtHomeVolumePointSettings : AbstractVolumePointSettings
	{
		public const string FACTORY_NAME = "KrangAtHomeVolumePoint";

		private const string AUDIO_OPTION_ELEMENT = "AudioOption";

		public eAudioOption AudioOption { get; set; }

		/// <summary>
		/// Write property elements to an xml writer.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(AUDIO_OPTION_ELEMENT, IcdXmlConvert.ToString(AudioOption));
		}

		/// <summary>
		/// Initialize volume point settings from an xml element.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			AudioOption = XmlUtils.TryReadChildElementContentAsEnum<eAudioOption>(xml, AUDIO_OPTION_ELEMENT, true) ?? eAudioOption.All;
		}

	}
}