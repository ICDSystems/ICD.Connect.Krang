using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Krang.SPlus.SPlusRoomInfo.Device
{
	[KrangSettings("SPlusRoomInfo", typeof(SPlusRoomInfo))]
	public sealed class SPlusRoomInfoSettings : AbstractDeviceSettings
	{

		#region Constants

		private const string ROOM_ID_ELEMENT = "RoomId";

		#endregion

		#region Properties

		public int RoomId { get; set; }

		#endregion

		#region Settings
		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			RoomId = XmlUtils.ReadChildElementContentAsInt(xml, ROOM_ID_ELEMENT);
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ROOM_ID_ELEMENT, IcdXmlConvert.ToString(RoomId));
		}

		#endregion
	}
}