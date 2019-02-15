using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Routing.Connections;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages
{
	public sealed class VideoEquipmentPage : AbstractAudioVideoEquipmentPage
	{
		public override eConnectionType ConnectionType { get { return eConnectionType.Video; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="equipment"></param>
		public VideoEquipmentPage(EquipmentCrosspoint equipment)
			: base(equipment)
		{
		}
	}
}
