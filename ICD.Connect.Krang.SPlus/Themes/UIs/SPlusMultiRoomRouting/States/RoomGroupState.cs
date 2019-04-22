using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Krang.SPlus.RoomGroups;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages;
using ICD.Connect.Protocol.Crosspoints;
using ICD.Connect.Protocol.Sigs;

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

		private readonly SigCache m_SigCache;
		private readonly SafeCriticalSection m_SigCacheSection;

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

			m_SigCache = new SigCache();
			m_SigCacheSection = new SafeCriticalSection();

			m_RoomStates = roomGroup.GetRooms().OfType<IKrangAtHomeRoom>().Select(r =>
			                                                                      {
				                                                                      var output = new RoomState(r);
				                                                                      Subscribe(output);
				                                                                      return output;
			                                                                      }).ToList();

			//Populate Initial Cache
			SetInitialSigCacheValues();
			
		}

		private void Subscribe(RoomState output)
		{
			output.OnMuteStateChanged += OutputOnMuteStateChanged;
			output.OnVolumePositionChanged += OutputOnVolumePositionChanged;
			output.OnActiveSourceChanged += OutputOnActiveSourceChanged;
			output.OnVolumeAvaliabilityChanged += OutputOnVolumeAvaliabilityChanged;
		}

		#region Controls

		public void AddControlId(int id, CrosspointData data)
		{
			m_ControlIdsSection.Enter();

			try
			{
				if (!m_ControlIds.Add(id))
					return;

				Update(id, data);
			}
			finally
			{
				m_ControlIdsSection.Leave();
			}
		}

		public void RemoveControlId(int id, CrosspointData data)
		{
			m_ControlIdsSection.Enter();

			try
			{
				if (!m_ControlIds.Remove(id))
					return;

				Clear(id, data);
			}
			finally
			{
				m_ControlIdsSection.Leave();
			}
		}

		#endregion

		#region Joins

		private void Update(int id, CrosspointData data)
		{
			data.AddSigs(GetSigCache());
		}

		private void Clear(int id, CrosspointData data)
		{
			Update(data, true);
		}

		private void Update(CrosspointData data, bool clear)
		{
			data.AddSig(Joins.SMARTOBJECT_ROOM_GROUP,
			            (ushort)(Joins.DYNAMIC_BUTTON_LIST_DIGITAL_START_SELECTED_JOIN + m_Index),
			            !clear);

			data.AddSig(Joins.SMARTOBJECT_ROOMS, Joins.SRL_NUMBER_OF_ITEMS_JOIN, clear ? (ushort)0 : (ushort)m_RoomStates.Count);

			//We send a digital to the main crosspoint if rooms are avaliable, as a convenence
			data.AddSig(0,Joins.DIGITAL_ROOMS_AVAILABLE, !clear && m_RoomStates.Count > 0);

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
				data.AddSig(Joins.SMARTOBJECT_ROOMS, mutedJoin, !clear && muted);

				// Volume
				ushort volume = (ushort)(roomState.VolumePostition * ushort.MaxValue);
				ushort volumeJoin = Joins.GetAnalogJoinOffset(index, Joins.ANALOG_ROOMS_OFFSET, Joins.ANALOG_ROOMS_VOLUME);
				data.AddSig(Joins.SMARTOBJECT_ROOMS, volumeJoin, clear ? (ushort)0 : volume);

				// Volume Avaliability
				bool volumeAvaliable = roomState.VolumeAvaliable;
				ushort volumeAvaliableJoin = Joins.GetDigitalJoinOffset(index, Joins.DIGITAL_ROOMS_OFFSET, Joins.DIGITAL_ROOMS_ENABLE_VOLUME);
				data.AddSig(Joins.SMARTOBJECT_ROOMS, volumeAvaliableJoin, !clear && volumeAvaliable);

			}
		}

		private void UpdateRoomVolumeState(RoomState state)
		{
			CrosspointData data = new CrosspointData();

			ushort index = (ushort)m_RoomStates.IndexOf(state);

			// Volume
			ushort volume = (ushort)(state.VolumePostition * ushort.MaxValue);
			ushort volumeJoin = Joins.GetAnalogJoinOffset(index, Joins.ANALOG_ROOMS_OFFSET, Joins.ANALOG_ROOMS_VOLUME);
			data.AddSig(Joins.SMARTOBJECT_ROOMS, volumeJoin, volume);

			SendInputData(data);
		}

		private void UpdateRoomMuteState(RoomState state)
		{
			CrosspointData data = new CrosspointData();

			ushort index = (ushort)m_RoomStates.IndexOf(state);

			// Mute
			bool muted = state.Muted;
			ushort mutedJoin = Joins.GetDigitalJoinOffset(index, Joins.DIGITAL_ROOMS_OFFSET, Joins.DIGITAL_ROOMS_MUTE);
			data.AddSig(Joins.SMARTOBJECT_ROOMS, mutedJoin, muted);

			SendInputData(data);
		}

		private void UpdateRoomVolumeAvaliableState(RoomState state)
		{
			CrosspointData data = new CrosspointData();

			ushort index = (ushort)m_RoomStates.IndexOf(state);

			// Volume Avaliability
			bool volumeAvaliable = state.VolumeAvaliable;
			ushort volumeAvaliableJoin = Joins.GetDigitalJoinOffset(index, Joins.DIGITAL_ROOMS_OFFSET, Joins.DIGITAL_ROOMS_ENABLE_VOLUME);
			data.AddSig(Joins.SMARTOBJECT_ROOMS, volumeAvaliableJoin, volumeAvaliable);

			SendInputData(data);
		}

		private void UpdateRoomSourceState(RoomState state)
		{
			CrosspointData data = new CrosspointData();

			ushort index = (ushort)m_RoomStates.IndexOf(state);

			// Source
			ushort sourceJoin = Joins.GetSerialJoinOffset(index, Joins.SERIAL_ROOMS_OFFSET, Joins.SERIAL_ROOMS_SOURCE);
			data.AddSig(Joins.SMARTOBJECT_ROOMS, sourceJoin, state.Source);

			SendInputData(data);
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

			UpdateRoomMuteState(state);
		}

		private void OutputOnActiveSourceChanged(object sender, EventArgs eventArgs)
		{
			RoomState state = sender as RoomState;

			UpdateRoomSourceState(state);
		}

		private void OutputOnVolumeAvaliabilityChanged(object sender, BoolEventArgs boolEventArgs)
		{
			RoomState state = sender as RoomState;

			UpdateRoomVolumeAvaliableState(state);
		}

		#endregion

		public bool ContainsControlId(int id)
		{
			return m_ControlIds.Contains(id);
		}

		public RoomState GetRoomStateAtIndex(int roomIndex)
		{
			return m_RoomStates[roomIndex];
		}

		public IEnumerable<SigInfo> GetSigCache()
		{
			return m_SigCacheSection.Execute(() => m_SigCache.ToList(m_SigCache.Count));
		}

		private void SendInputData(CrosspointData data)
		{
			m_SigCacheSection.Execute(() => m_SigCache.AddHighRemoveLow(data.GetSigs()));


			// If there are no controls for this RoomGroupState, don't send anything
			bool hasControls = false;
			m_ControlIdsSection.Enter();
			try
			{
				if (m_ControlIds.Count > 0)
				{
					hasControls = true;
					data.AddControlIds(m_ControlIds);
				}
			}
			finally
			{
				m_ControlIdsSection.Leave();
			}

			if (hasControls)
				m_Page.Equipment.SendInputData(data);
		}

		private void SetInitialSigCacheValues()
		{
			CrosspointData data = new CrosspointData();
			Update(data, false);
			m_SigCacheSection.Execute(() => m_SigCache.AddRange(data.GetSigs()));
		}
	}
}
