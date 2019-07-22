using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Originators.Simpl;

namespace ICD.Connect.Krang.SPlus.SPlusRoomGroupControl.Device
{
	[KrangSettings("SPlusRoomGroupControl", typeof(SPlusRoomGroupControlDevice))]
	public sealed class SPlusRoomGroupControlDeviceSettings : AbstractDeviceSettings, ISimplOriginatorSettings
	{

		private const string ROOM_GROUP_ELEMENT = "RoomGroup";

		public int RoomGroupId { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ROOM_GROUP_ELEMENT, IcdXmlConvert.ToString(RoomGroupId));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			RoomGroupId = XmlUtils.ReadChildElementContentAsInt(xml, ROOM_GROUP_ELEMENT);
		}
	}
}