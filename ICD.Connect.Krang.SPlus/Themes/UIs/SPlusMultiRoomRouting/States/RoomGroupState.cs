using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages;
using ICD.Connect.Protocol.Crosspoints;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.States
{
	public sealed class RoomGroupState
	{
		private readonly IcdHashSet<int> m_ControlIds;
		private readonly SafeCriticalSection m_ControlIdsSection;

		private readonly int m_RoomGroupId;
		private readonly AbstractAudioVideoEquipmentPage m_Page;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="roomGroupId"></param>
		public RoomGroupState(AbstractAudioVideoEquipmentPage page, int roomGroupId)
		{
			m_Page = page;
			m_RoomGroupId = roomGroupId;

			m_ControlIds = new IcdHashSet<int>();
			m_ControlIdsSection = new SafeCriticalSection();
		}

		public void AddControlId(int id)
		{
			m_ControlIdsSection.Enter();

			try
			{
				if (!m_ControlIds.Add(id))
					return;

				Initialize(id);
			}
			finally
			{
				m_ControlIdsSection.Leave();
			}
		}

		public void RemoveControlId(int id)
		{
			m_ControlIdsSection.Execute(() => m_ControlIds.Remove(id));
		}

		private void Initialize(int id)
		{
			CrosspointData data = new CrosspointData();

			data.AddControlId(id);

			data.AddSig(0, Joins.DIGITAL_ROOMS_GROUP_1_PRESS, m_RoomGroupId == 1);
			data.AddSig(0, Joins.DIGITAL_ROOMS_GROUP_2_PRESS, m_RoomGroupId == 2);
			data.AddSig(0, Joins.DIGITAL_ROOMS_GROUP_3_PRESS, m_RoomGroupId == 3);

			m_Page.Equipment.SendInputData(data);
		}
	}
}
