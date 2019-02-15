using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Krang.SPlus.RoomGroups;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.States;
using ICD.Connect.Protocol.Crosspoints;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Sigs;
using ICD.Connect.Routing.Connections;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages
{
	public sealed class AudioVideoEquipmentPage : IDisposable
	{
		private readonly Dictionary<int, ControlCrosspointState> m_Sessions;
		private readonly Dictionary<int, RoomGroupState> m_RoomGroupStates;
		private readonly IcdOrderedDictionary<int, SPlusRoomGroup> m_IndexToRoomGroup;
		private readonly List<IKrangAtHomeSource> m_Sources;

		private readonly SafeCriticalSection m_SessionsSection;

		private readonly EquipmentCrosspoint m_Equipment;
		private readonly KrangAtHomeTheme m_Theme;

		public EquipmentCrosspoint Equipment { get { return m_Equipment; } }
		public KrangAtHomeTheme Theme { get { return m_Theme; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="theme"></param>
		/// <param name="equipment"></param>
		/// <param name="connectionType"></param>
		public AudioVideoEquipmentPage(KrangAtHomeTheme theme, EquipmentCrosspoint equipment, eConnectionType connectionType)
		{
			if (equipment == null)
				throw new ArgumentNullException("equipment");

			m_Sessions = new Dictionary<int, ControlCrosspointState>();
			m_RoomGroupStates = new Dictionary<int, RoomGroupState>();
			m_SessionsSection = new SafeCriticalSection();

			m_IndexToRoomGroup =
				new IcdOrderedDictionary<int, SPlusRoomGroup>(theme.Core
				                                                   .Originators
				                                                   .GetChildren<SPlusRoomGroup>()
				                                                   .ToDictionary(g => g.Index,
				                                                                 g => g));

			switch (connectionType)
			{
				case eConnectionType.Audio:
					m_Sources = theme.GetAudioSources().ToList();
					break;

				case eConnectionType.Video:
					m_Sources = theme.GetVideoSources().ToList();
					break;

				default:
					throw new ArgumentOutOfRangeException("connectionType");
			}

			m_Theme = theme;
			Subscribe(m_Theme);

			m_Equipment = equipment;
			Subscribe(m_Equipment);
		}

		public void Dispose()
		{
			Unsubscribe(m_Theme);
			Unsubscribe(m_Equipment);
		}

		#region Joins

		/// <summary>
		/// Send the common page information to the controls.
		/// </summary>
		private void UpdateAll()
		{
			CrosspointData data = new CrosspointData();
			data.AddControlIds(Equipment.ControlCrosspoints);

			Update(data);

			Equipment.SendInputData(data);
		}

		/// <summary>
		/// Send the common page information to the control with the given id.
		/// </summary>
		/// <param name="id"></param>
		private void Update(int id)
		{
			CrosspointData data = new CrosspointData();
			data.AddControlId(id);

			Update(data);

			Equipment.SendInputData(data);
		}

		private void Update(CrosspointData data)
		{
			UpdateSources(data);
			UpdateRoomGroups(data);
		}

		private void UpdateSources(CrosspointData data)
		{
			data.AddSig(Joins.SMARTOBJECT_SOURCES, Joins.SRL_NUMBER_OF_ITEMS_JOIN, (ushort)m_Sources.Count);

			for (ushort index = 0; index < m_Sources.Count; index++)
			{
				IKrangAtHomeSource source = m_Sources[index];
				string[] rooms = m_Theme.KrangAtHomeRouting
										.KrangAtHomeRoutingCache
										.GetRoomsForSource(source)
										.Select(r => r.Name)
										.ToArray();

				string name = source.Name;
				string roomsFeedback = string.Join(", ", rooms);

				ushort nameJoin = Joins.GetSerialJoinOffset(index, Joins.SERIAL_SOURCES_OFFSET, Joins.SERIAL_SOURCES_NAME);
				ushort roomsFeedbackJoin = Joins.GetSerialJoinOffset(index, Joins.SERIAL_SOURCES_OFFSET, Joins.SERIAL_SOURCES_ROOMS);

				data.AddSig(Joins.SMARTOBJECT_SOURCES, nameJoin, name);
				data.AddSig(Joins.SMARTOBJECT_SOURCES, roomsFeedbackJoin, roomsFeedback);
			}
		}

		private void UpdateRoomGroups(CrosspointData data)
		{
			data.AddSig(Joins.SMARTOBJECT_ROOM_GROUP, Joins.DYNAMIC_BUTTON_LIST_NUMBER_OF_ITEMS_JOIN, (ushort)m_IndexToRoomGroup.Count);

			foreach (var kvp in m_IndexToRoomGroup)
			{
				ushort index = (ushort)kvp.Key;
				SPlusRoomGroup roomGroup = kvp.Value;
				string name = roomGroup.Name;

				data.AddSig(Joins.SMARTOBJECT_ROOM_GROUP, (ushort)(Joins.DYNAMIC_BUTTON_LIST_SERIAL_START_TEXT_JOIN + index), name);
			}
		}

		#endregion

		#region Private Methods

		private ControlCrosspointState LazyLoadCrosspointState(int id)
		{
			m_SessionsSection.Enter();

			try
			{
				ControlCrosspointState crosspointState;
				if (!m_Sessions.TryGetValue(id, out crosspointState))
				{
					crosspointState = new ControlCrosspointState(this, id);
					m_Sessions.Add(id, crosspointState);

					SetRoomGroup(crosspointState, 1);

					Update(id);
				}

				return crosspointState;
			}
			finally
			{
				m_SessionsSection.Leave();
			}
		}

		private void SetRoomGroup(ControlCrosspointState crosspointState, int index)
		{
			m_SessionsSection.Enter();

			try
			{
				foreach (RoomGroupState state in m_RoomGroupStates.Values)
					state.RemoveControlId(crosspointState.ControlCrosspointId);

				RoomGroupState roomGroupState;
				if (!m_RoomGroupStates.TryGetValue(index, out roomGroupState))
				{
					roomGroupState = new RoomGroupState(this, index, m_IndexToRoomGroup[index]);
					m_RoomGroupStates.Add(index, roomGroupState);
				}

				roomGroupState.AddControlId(crosspointState.ControlCrosspointId);
			}
			finally
			{
				m_SessionsSection.Leave();
			}
		}

		private IEnumerable<IKrangAtHomeSource> GetSources()
		{
			return m_Sources;
		}

		private IEnumerable<SPlusRoomGroup> GetRoomGroups()
		{
			return m_IndexToRoomGroup.Values;
		}

		public IEnumerable<IKrangAtHomeRoom> GetRooms(int group)
		{
			SPlusRoomGroup roomGroup;
			return m_IndexToRoomGroup.TryGetValue(group, out roomGroup)
					   ? roomGroup.GetRooms().OfType<IKrangAtHomeRoom>()
					   : Enumerable.Empty<IKrangAtHomeRoom>();
		}

		#endregion

		#region Theme Callbacks

		private void Subscribe(KrangAtHomeTheme theme)
		{
			theme.KrangAtHomeRouting.OnSourceRoomsUsedUpdated += KrangAtHomeRoutingOnSourceRoomsUsedUpdated;
		}

		private void Unsubscribe(KrangAtHomeTheme theme)
		{
			theme.KrangAtHomeRouting.OnSourceRoomsUsedUpdated -= KrangAtHomeRoutingOnSourceRoomsUsedUpdated;
		}

		private void KrangAtHomeRoutingOnSourceRoomsUsedUpdated(object sender, SourceRoomsUsedUpdatedEventArgs eventArgs)
		{
			UpdateAll();
		}

		#endregion

		#region Equipment Callbacks

		private void Subscribe(EquipmentCrosspoint equipment)
		{
			equipment.OnControlCrosspointCountChanged += EquipmentOnControlCrosspointCountChanged;
			equipment.OnSendOutputData += EquipmentOnSendOutputData;
		}

		private void Unsubscribe(EquipmentCrosspoint equipment)
		{
			equipment.OnControlCrosspointCountChanged -= EquipmentOnControlCrosspointCountChanged;
			equipment.OnSendOutputData -= EquipmentOnSendOutputData;
		}

		private void EquipmentOnControlCrosspointCountChanged(object sender, IntEventArgs eventArgs)
		{
			foreach (int id in m_Equipment.ControlCrosspoints)
				LazyLoadCrosspointState(id);
		}

		private void EquipmentOnSendOutputData(ICrosspoint sender, CrosspointData data)
		{
			foreach (int id in data.GetControlIds())
				foreach (var item in data.GetSigs())
					HandleSigFromControl(id, item);
		}

		private void HandleSigFromControl(int id, SigInfo sig)
		{
			ControlCrosspointState crosspointState = LazyLoadCrosspointState(id);

			// Select source
			if (sig.SmartObject == Joins.SMARTOBJECT_SOURCES && sig.Type == eSigType.Digital)
			{
				
			}

			// Select room

			// Volume up

			// Volume down

			// Mute toggle

			// Room group
			if (sig.SmartObject == Joins.SMARTOBJECT_ROOM_GROUP && sig.Type == eSigType.Analog)
			{
				
			}

			// Control source
			if (sig.SmartObject == 0 && sig.Type == eSigType.Digital && sig.Number == Joins.DIGITAL_CONTROL_SOURCE)
			{

			}

			// Off
			if (sig.SmartObject == 0 && sig.Type == eSigType.Digital && sig.Number == Joins.DIGITAL_OFF)
			{
				
			}
		}

		#endregion
	}
}
