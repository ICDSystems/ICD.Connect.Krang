using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.States
{
	public sealed class ControlCrosspointState
	{
		private readonly AudioVideoEquipmentPage m_Page;
		private readonly int m_ControlCrosspointId;
		private int? m_SelectedRoom;
		private int? m_SelectedSource;

		public int ControlCrosspointId { get { return m_ControlCrosspointId; } }

		public AudioVideoEquipmentPage Page { get { return m_Page; } }

		public int? SelectedRoom
		{
			get { return m_SelectedRoom; }
			set
			{
				m_SelectedRoom = value;


			}
		}

		public int? SelectedSource { get { return m_SelectedSource; } set { m_SelectedSource = value; } }

		public ControlCrosspointState(AudioVideoEquipmentPage audioVideoEquipmentPage, int id)
		{
			m_Page = audioVideoEquipmentPage;
			m_ControlCrosspointId = id;
		}
	}
}
