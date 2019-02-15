using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages;
using ICD.Connect.Protocol.Crosspoints;

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
				if (value == m_SelectedRoom)
					return;

				int? old = m_SelectedRoom;
				m_SelectedRoom = value;

				CrosspointData data = new CrosspointData();
				data.AddControlId(m_ControlCrosspointId);

				if (old != null)
				{
					ushort buttonJoin = Joins.GetDigitalJoinOffset(old.Value, Joins.DIGITAL_ROOMS_OFFSET,
					                                               Joins.DIGITAL_ROOMS_SELECT);
					data.AddSig(Joins.SMARTOBJECT_ROOMS, buttonJoin, false);
				}

				if (m_SelectedRoom != null)
				{
					ushort buttonJoin = Joins.GetDigitalJoinOffset(m_SelectedRoom.Value, Joins.DIGITAL_ROOMS_OFFSET,
																   Joins.DIGITAL_ROOMS_SELECT);
					data.AddSig(Joins.SMARTOBJECT_ROOMS, buttonJoin, false);
				}

				m_Page.Equipment.SendInputData(data);
			}
		}

		public int? SelectedSource
		{
			get
			{
				return m_SelectedSource;
			}
			set
			{
				int? old = m_SelectedSource;
				m_SelectedSource = value;

				CrosspointData data = new CrosspointData();
				data.AddControlId(m_ControlCrosspointId);

				if (old != null)
				{
					ushort buttonJoin = Joins.GetDigitalJoinOffset(old.Value, Joins.DIGITAL_SOURCES_OFFSET,
																   Joins.DIGITAL_SOURCES_SELECT);
					data.AddSig(Joins.SMARTOBJECT_SOURCES, buttonJoin, false);
				}

				if (m_SelectedSource != null)
				{
					ushort buttonJoin = Joins.GetDigitalJoinOffset(m_SelectedSource.Value, Joins.DIGITAL_SOURCES_OFFSET,
																   Joins.DIGITAL_SOURCES_SELECT);
					data.AddSig(Joins.SMARTOBJECT_SOURCES, buttonJoin, false);
				}

				m_Page.Equipment.SendInputData(data);
			}
		}

		public ControlCrosspointState(AudioVideoEquipmentPage audioVideoEquipmentPage, int id)
		{
			m_Page = audioVideoEquipmentPage;
			m_ControlCrosspointId = id;
		}
	}
}
