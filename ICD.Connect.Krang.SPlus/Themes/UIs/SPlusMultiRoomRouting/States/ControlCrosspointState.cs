using ICD.Common.Utils.Collections;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages;
using ICD.Connect.Protocol.Crosspoints;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.States
{
	public sealed class ControlCrosspointState
	{
		private readonly AudioVideoEquipmentPage m_Page;
		private readonly int m_ControlCrosspointId;
		private int? m_SelectedSource;

		public int ControlCrosspointId { get { return m_ControlCrosspointId; } }

		public AudioVideoEquipmentPage Page { get { return m_Page; } }

		private readonly IcdHashSet<int> m_SelectedRooms;

		public void AddSelectedRoom(int index)
		{
			if (!m_SelectedRooms.Add(index))
				return;

			CrosspointData data = new CrosspointData();
			data.AddControlId(m_ControlCrosspointId);

			ushort buttonJoin = Joins.GetDigitalJoinOffset(index, Joins.DIGITAL_ROOMS_OFFSET, Joins.DIGITAL_ROOMS_SELECT);
			data.AddSig(Joins.SMARTOBJECT_ROOMS, buttonJoin, true);

			m_Page.Equipment.SendInputData(data);
		}

		public void RemoveSelectedRoom(int index)
		{
			if (!m_SelectedRooms.Remove(index))
				return;

			CrosspointData data = new CrosspointData();
			data.AddControlId(m_ControlCrosspointId);

			ushort buttonJoin = Joins.GetDigitalJoinOffset(index, Joins.DIGITAL_ROOMS_OFFSET, Joins.DIGITAL_ROOMS_SELECT);
			data.AddSig(Joins.SMARTOBJECT_ROOMS, buttonJoin, false);

			m_Page.Equipment.SendInputData(data);
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
			m_SelectedRooms = new IcdHashSet<int>();
		}
	}
}
