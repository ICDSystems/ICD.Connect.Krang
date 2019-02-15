using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Routing.Connections;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages
{
	public sealed class AudioEquipmentPage : AbstractAudioVideoEquipmentPage
	{
		public override eConnectionType ConnectionType { get { return eConnectionType.Audio; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="equipment"></param>
		public AudioEquipmentPage(EquipmentCrosspoint equipment)
			: base(equipment)
		{
		}
	}
}
