using ICD.Common.Utils.Xml;
using ICD.Connect.Partitioning.RoomGroups;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Krang.SPlus.SPlusRoomGroup
{
	[KrangSettings("SPlusRoomGroup", typeof (SPlusRoomGroup))]
	public sealed class SPlusRoomGroupSettings : AbstractRoomGroupSettings
	{
		private const string INDEX_ELEMENT = "Index";

		public int Index { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(INDEX_ELEMENT, IcdXmlConvert.ToString(Index));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Index = XmlUtils.ReadChildElementContentAsInt(xml, INDEX_ELEMENT);
		}
	}
}