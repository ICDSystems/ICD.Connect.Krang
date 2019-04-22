using System.Collections.Generic;
using ICD.Common.Utils.Collections;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
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

		public void ToggleSelectedRoom(int index)
		{
			if (m_SelectedRooms.Contains(index))
				RemoveSelectedRoom(index);
			else
				AddSelectedRoom(index);
		}

		public void AddSelectedRoom(int index)
		{
			if (!m_SelectedRooms.Add(index))
				return;

			CrosspointData data = new CrosspointData();
			data.AddControlId(m_ControlCrosspointId);

			ushort buttonJoin = Joins.GetDigitalJoinOffset(index, Joins.DIGITAL_ROOMS_OFFSET, Joins.DIGITAL_ROOMS_SELECT);
			data.AddSig(Joins.SMARTOBJECT_ROOMS, buttonJoin, true);

			SetSelectedSource(null, data);

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

			SetSelectedSource(null, data);

			m_Page.Equipment.SendInputData(data);
		}

		public void ClearSelectedRooms()
		{
			if (m_SelectedRooms.Count == 0)
				return;

			CrosspointData data = new CrosspointData();
			data.AddControlId(m_ControlCrosspointId);

			ClearSelectedRooms(data);

			m_Page.Equipment.SendInputData(data);
		}

		public void ClearSelectedRooms(CrosspointData data)
		{
			if (m_SelectedRooms.Count == 0)
				return;

			foreach (int index in m_SelectedRooms)
			{
				ushort buttonJoin = Joins.GetDigitalJoinOffset(index, Joins.DIGITAL_ROOMS_OFFSET, Joins.DIGITAL_ROOMS_SELECT);
				data.AddSig(Joins.SMARTOBJECT_ROOMS, buttonJoin, false);
			}

			m_SelectedRooms.Clear();
		}

		public void SetSelectedSource(int? index, CrosspointData data)
		{
			m_SelectedSource = index;

			if (m_SelectedSource != null)
			{
				var source = m_Page.GetSource(m_SelectedSource.Value);
				if (source != null)
				{
					data.AddSig(0, Joins.ANALOG_SOURCE_SELECTED_CROSSPOINT_ID, source.CrosspointId);
					data.AddSig(0, Joins.ANALOG_SOURCE_SELECTED_CROSSPOINT_TYPE, source.CrosspointType);
					data.AddSig(0, Joins.SERIAL_SOURCE_SELECTED_NAME, source.Name);
				}
				else
				{
					data.AddSig(0, Joins.ANALOG_SOURCE_SELECTED_CROSSPOINT_ID, 0);
					data.AddSig(0, Joins.ANALOG_SOURCE_SELECTED_CROSSPOINT_TYPE, 0);
					data.AddSig(0, Joins.SERIAL_SOURCE_SELECTED_NAME, null);
				}
			}
			else
			{
				data.AddSig(0, Joins.ANALOG_SOURCE_SELECTED_CROSSPOINT_ID, 0);
				data.AddSig(0, Joins.ANALOG_SOURCE_SELECTED_CROSSPOINT_TYPE, 0);
				data.AddSig(0, Joins.SERIAL_SOURCE_SELECTED_NAME, null);
			}
		}

		public void SetSelectedSource(int? index)
		{
			CrosspointData data = new CrosspointData();
			data.AddControlId(m_ControlCrosspointId);
			
			SetSelectedSource(index, data);

			m_Page.Equipment.SendInputData(data);
		}

		public ControlCrosspointState(AudioVideoEquipmentPage audioVideoEquipmentPage, int id)
		{
			m_Page = audioVideoEquipmentPage;
			m_ControlCrosspointId = id;
			m_SelectedRooms = new IcdHashSet<int>();
		}

		public IEnumerable<int> GetSelectedRooms()
		{
			return m_SelectedRooms;
		}
	}
}
