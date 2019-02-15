using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Krang.SPlus.RoomGroups;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages;
using ICD.Connect.Protocol.Crosspoints;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.States
{
	public sealed class RoomGroupState
	{
		private readonly IcdHashSet<int> m_ControlIds;
		private readonly SafeCriticalSection m_ControlIdsSection;

		private readonly SPlusRoomGroup m_RoomGroup;
		private readonly AudioVideoEquipmentPage m_Page;

		private readonly List<RoomState> m_RoomStates;
		private readonly int m_Index;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="index"></param>
		/// <param name="roomGroup"></param>
		public RoomGroupState(AudioVideoEquipmentPage page, int index, SPlusRoomGroup roomGroup)
		{
			m_Page = page;
			m_Index = index;
			m_RoomGroup = roomGroup;

			m_ControlIds = new IcdHashSet<int>();
			m_ControlIdsSection = new SafeCriticalSection();

			m_RoomStates = roomGroup.GetRooms().OfType<IKrangAtHomeRoom>().Select(r =>
			                                                                      {
				                                                                      var output = new RoomState(r);
				                                                                      Subscribe(output);
				                                                                      return output;
			                                                                      }).ToList();
		}

		private void Subscribe(RoomState output)
		{
			output.OnMuteStateChanged += OutputOnMuteStateChanged;
			output.OnVolumePositionChanged += OutputOnVolumePositionChanged;
		}

		#region Controls

		public void AddControlId(int id)
		{
			m_ControlIdsSection.Enter();

			try
			{
				if (!m_ControlIds.Add(id))
					return;

				Update(id);
			}
			finally
			{
				m_ControlIdsSection.Leave();
			}
		}

		public void RemoveControlId(int id)
		{
			m_ControlIdsSection.Enter();

			try
			{
				if (!m_ControlIds.Remove(id))
					return;

				Clear(id);
			}
			finally
			{
				m_ControlIdsSection.Leave();
			}
		}

		#endregion

		#region Joins

		private void Update(int id)
		{
			CrosspointData data = new CrosspointData();
			data.AddControlId(id);

			Update(data, false);

			m_Page.Equipment.SendInputData(data);
		}

		private void Clear(int id)
		{
			CrosspointData data = new CrosspointData();
			data.AddControlId(id);

			Update(data, true);

			m_Page.Equipment.SendInputData(data);
		}

		private void Update(CrosspointData data, bool clear)
		{
			data.AddSig(Joins.SMARTOBJECT_ROOM_GROUP,
			            (ushort)(Joins.DYNAMIC_BUTTON_LIST_DIGITAL_START_SELECTED_JOIN + m_Index),
			            !clear);

			data.AddSig(Joins.SMARTOBJECT_ROOMS, Joins.SRL_NUMBER_OF_ITEMS_JOIN, clear ? (ushort)0 : (ushort)m_RoomStates.Count);

			for (ushort index = 0; index < m_RoomStates.Count; index++)
			{
				RoomState roomState = m_RoomStates[index];
				string name = roomState.Room.Name;
				string sourcesFeedback = roomState.Room.GetSource() == null ? null : roomState.Room.GetSource().Name;

				// Labels
				ushort nameJoin = Joins.GetSerialJoinOffset(index, Joins.SERIAL_ROOMS_OFFSET, Joins.SERIAL_ROOMS_NAME);
				ushort roomsFeedbackJoin = Joins.GetSerialJoinOffset(index, Joins.SERIAL_ROOMS_OFFSET, Joins.SERIAL_ROOMS_SOURCE);

				data.AddSig(Joins.SMARTOBJECT_ROOMS, nameJoin, clear ? null : name);
				data.AddSig(Joins.SMARTOBJECT_ROOMS, roomsFeedbackJoin, clear ? null : sourcesFeedback);

				// Mute
				bool muted = roomState.Muted;
				ushort mutedJoin = Joins.GetDigitalJoinOffset(index, Joins.DIGITAL_ROOMS_OFFSET, Joins.DIGITAL_ROOMS_MUTE);
				data.AddSig(Joins.SMARTOBJECT_ROOMS, mutedJoin, clear ? false : muted);

				// Volume
				ushort volume = (ushort)(roomState.VolumePostition * ushort.MaxValue);
				ushort volumeJoin = Joins.GetAnalogJoinOffset(index, Joins.ANALOG_ROOMS_OFFSET, Joins.ANALOG_ROOMS_VOLUME);
				data.AddSig(Joins.SMARTOBJECT_ROOMS, volumeJoin, clear ? (ushort)0 : volume);
			}
		}

		private void UpdateRoomVolumeState(RoomState state)
		{
			CrosspointData data = new CrosspointData();
			data.AddControlIds(m_ControlIds);

			ushort index = (ushort)m_RoomStates.IndexOf(state);

			// Mute
			bool muted = state.Muted;
			ushort mutedJoin = Joins.GetDigitalJoinOffset(index, Joins.DIGITAL_ROOMS_OFFSET, Joins.DIGITAL_ROOMS_MUTE);
			data.AddSig(Joins.SMARTOBJECT_ROOMS, mutedJoin, muted);

			// Volume
			ushort volume = (ushort)(state.VolumePostition * ushort.MaxValue);
			ushort volumeJoin = Joins.GetAnalogJoinOffset(index, Joins.ANALOG_ROOMS_OFFSET, Joins.ANALOG_ROOMS_VOLUME);
			data.AddSig(Joins.SMARTOBJECT_ROOMS, volumeJoin, volume);

			m_Page.Equipment.SendInputData(data);
		}

		#endregion

		#region RoomState Callbacks

		private void OutputOnVolumePositionChanged(object sender, FloatEventArgs floatEventArgs)
		{
			RoomState state = sender as RoomState;

			UpdateRoomVolumeState(state);
		}

		private void OutputOnMuteStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			RoomState state = sender as RoomState;

			UpdateRoomVolumeState(state);
		}

		#endregion
	}
}
