using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Krang.SPlus.RoomGroups;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.States;
using ICD.Connect.Protocol.Crosspoints;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Sigs;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages
{
	public sealed class AudioVideoEquipmentPage : IDisposable, IConsoleNode
	{
		private readonly Dictionary<int, ControlCrosspointState> m_Sessions;
		private readonly Dictionary<int, RoomGroupState> m_RoomGroupStates;
		private readonly IcdOrderedDictionary<int, SPlusRoomGroup> m_IndexToRoomGroup;
		private readonly List<IKrangAtHomeSource> m_Sources;

		private readonly SafeCriticalSection m_SessionsSection;

		private readonly NonCachingEquipmentCrosspoint m_Equipment;
		private readonly KrangAtHomeTheme m_Theme;
		private readonly eConnectionType m_ConnectionType;

		private readonly SigCache m_SigCache;
		private readonly SafeCriticalSection m_SigCacheSection;

		public NonCachingEquipmentCrosspoint Equipment { get { return m_Equipment; } }
		public KrangAtHomeTheme Theme { get { return m_Theme; } }


		public AudioVideoEquipmentPage(KrangAtHomeTheme theme, KrangAtHomeMultiRoomRouting multiRoomRouting)
		{
			if (multiRoomRouting == null)
				throw new ArgumentNullException("equipment");

			m_Sessions = new Dictionary<int, ControlCrosspointState>();
			m_RoomGroupStates = new Dictionary<int, RoomGroupState>();
			m_SessionsSection = new SafeCriticalSection();

			m_SigCache = new SigCache();
			m_SigCacheSection = new SafeCriticalSection();

			m_ConnectionType = multiRoomRouting.ConnectionType;

			m_Sources = new List<IKrangAtHomeSource>(multiRoomRouting.Sources.OrderBy(s => ((ISource)s).Order));

			m_IndexToRoomGroup = new IcdOrderedDictionary<int, SPlusRoomGroup>(multiRoomRouting.RoomGroups);

			m_Theme = theme;
			Subscribe(m_Theme);

			m_Equipment = multiRoomRouting.EquipmentCrosspoint;
			Subscribe(m_Equipment);

			SetInitialSigCacheValues();
		}

		public void Dispose()
		{
			Unsubscribe(m_Theme);
			Unsubscribe(m_Equipment);
		}

		#region Joins

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
										.Where(r => !r.Hide)
										.Select(r => r.Name)
										.ToArray();

				string name = source.Name;
				string roomsFeedback = string.Join(", ", rooms);

				ushort nameJoin = Joins.GetSerialJoinOffset(index, Joins.SERIAL_SOURCES_OFFSET, Joins.SERIAL_SOURCES_NAME);
				ushort roomsFeedbackJoin = Joins.GetSerialJoinOffset(index, Joins.SERIAL_SOURCES_OFFSET, Joins.SERIAL_SOURCES_ROOMS);
				ushort iconJoin = Joins.GetSerialJoinOffset(index, Joins.SERIAL_SOURCES_OFFSET, Joins.SERIAL_SOURCES_ICON);

				data.AddSig(Joins.SMARTOBJECT_SOURCES, nameJoin, name);
				data.AddSig(Joins.SMARTOBJECT_SOURCES, roomsFeedbackJoin, roomsFeedback);
				data.AddSig(Joins.SMARTOBJECT_SOURCES, iconJoin, source.SourceIcon.GetStringForIcon(false));
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

		internal IKrangAtHomeSource GetSource(int index)
		{
			return m_Sources[index];
		}

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
			CrosspointData data = new CrosspointData();
			data.AddControlId(crosspointState.ControlCrosspointId);

			m_SessionsSection.Enter();

			try
			{
				foreach (RoomGroupState state in m_RoomGroupStates.Values)
					state.RemoveControlId(crosspointState.ControlCrosspointId, data);

				crosspointState.ClearSelectedRooms(data);

				RoomGroupState roomGroupState;
				if (!m_RoomGroupStates.TryGetValue(index, out roomGroupState))
				{
					roomGroupState = new RoomGroupState(this, index, m_IndexToRoomGroup[index]);
					m_RoomGroupStates.Add(index, roomGroupState);
				}

				roomGroupState.AddControlId(crosspointState.ControlCrosspointId, data);
			}
			finally
			{
				m_SessionsSection.Leave();
			}

			Equipment.SendInputData(data);
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
			IKrangAtHomeSource source = eventArgs.Source as IKrangAtHomeSource;
			if (source == null)
				return;

			CrosspointData data = new CrosspointData();
			int index = m_Sources.IndexOf(source);

			string roomsFeedback;
			if (eventArgs.RoomsInUse != null)
				roomsFeedback = string.Join(", ",eventArgs.RoomsInUse.Where(r => !r.Hide).Select(r => r.Name).ToArray());
			else
				roomsFeedback = null;

			ushort roomsFeedbackJoin = Joins.GetSerialJoinOffset(index, Joins.SERIAL_SOURCES_OFFSET, Joins.SERIAL_SOURCES_ROOMS);

			data.AddSig(Joins.SMARTOBJECT_SOURCES, roomsFeedbackJoin, roomsFeedback);

			SendInputData(data);
		}

		#endregion

		#region Equipment Callbacks

		private void Subscribe(NonCachingEquipmentCrosspoint equipment)
		{
			equipment.OnSendOutputData += EquipmentOnSendOutputData;
			equipment.OnControlCrosspointConnected += EquipmentOnControlCrosspointConnected;
			equipment.GetInitialSigs = GetInitialSigs;
		}

		private void Unsubscribe(NonCachingEquipmentCrosspoint equipment)
		{
			equipment.OnSendOutputData -= EquipmentOnSendOutputData;
			equipment.OnControlCrosspointConnected -= EquipmentOnControlCrosspointConnected;
			equipment.GetInitialSigs = null;
		}

		private void EquipmentOnSendOutputData(ICrosspoint sender, CrosspointData data)
		{
			foreach (int id in data.GetControlIds())
				foreach (var item in data.GetSigs())
					HandleSigFromControl(id, item);
		}

		private void EquipmentOnControlCrosspointConnected(object sender, IntEventArgs args)
		{
			LazyLoadCrosspointState(args.Data);
		}

		private IEnumerable<SigInfo> GetInitialSigs(int controlId)
		{
			return GetSigCache();
		}

		private void HandleSigFromControl(int id, SigInfo sig)
		{
			ControlCrosspointState crosspointState = LazyLoadCrosspointState(id);
			RoomGroupState roomGroupState;
			m_RoomGroupStates.Values.TryFirst(s => s.ContainsControlId(id), out roomGroupState);

			// Control source
			if (sig.SmartObject == 0 && sig.Type == eSigType.Digital && sig.Number == Joins.DIGITAL_CONTROL_SOURCE && sig.GetBoolValue())
			{
				// Do nothing
			}

			// Off
			if (roomGroupState != null && sig.SmartObject == 0 && sig.Type == eSigType.Digital && sig.Number == Joins.DIGITAL_OFF && sig.GetBoolValue())
			{
				// Unroute selected rooms
				foreach (int roomIndex in crosspointState.GetSelectedRooms())
				{
					IKrangAtHomeRoom room = roomGroupState.GetRoomStateAtIndex(roomIndex).Room;

					eSourceTypeRouted type = m_ConnectionType == eConnectionType.Audio
												 ? eSourceTypeRouted.Audio
												 : eSourceTypeRouted.AudioVideo;

					room.SetSource(null, type);
				}

				// Update Selected Source

				crosspointState.ClearSelectedRooms();
			}

			// Room Group Select via Analog Direct
			if (sig.SmartObject == 0 && sig.Type == eSigType.Analog && sig.Number == Joins.ANALOG_ROOM_GROUP_DIRECT)
			{
				int index = sig.GetUShortValue();
				SetRoomGroup(crosspointState, index);
			}

			// Select source
			if (roomGroupState != null && sig.SmartObject == Joins.SMARTOBJECT_SOURCES && sig.Type == eSigType.Digital && sig.GetBoolValue())
			{
				int index;
				ushort join = Joins.GetDigitalJoinFromOffset((ushort)sig.Number, Joins.DIGITAL_SOURCES_OFFSET, out index);

				if (join == Joins.DIGITAL_SOURCES_SELECT)
				{
					crosspointState.SetSelectedSource(index);

					IKrangAtHomeSource source = m_Sources[index];

                    foreach (int roomIndex in crosspointState.GetSelectedRooms())
                    {
						IKrangAtHomeRoom room = roomGroupState.GetRoomStateAtIndex(roomIndex).Room;

	                    eSourceTypeRouted type = m_ConnectionType == eConnectionType.Audio
		                                             ? eSourceTypeRouted.Audio
		                                             : eSourceTypeRouted.AudioVideo;

						room.SetSource(source, type);
                    }

					crosspointState.ClearSelectedRooms();
				}
			}

			if (roomGroupState != null && sig.SmartObject == Joins.SMARTOBJECT_ROOMS && sig.Type == eSigType.Digital)
			{
				int index;
				ushort join = Joins.GetDigitalJoinFromOffset((ushort)sig.Number, Joins.DIGITAL_ROOMS_OFFSET, out index);

				// Select room
				if (join == Joins.DIGITAL_ROOMS_SELECT && sig.GetBoolValue())
				{
					crosspointState.ToggleSelectedRoom(index);
				}

				// Volume up
				if (join == Joins.DIGITAL_ROOMS_VOLUME_UP && sig.GetBoolValue())
				{
					roomGroupState.GetRoomStateAtIndex(index).VolumeUp();
				}

				// Volume down
				if (join == Joins.DIGITAL_ROOMS_VOLUME_DOWN && sig.GetBoolValue())
				{
					roomGroupState.GetRoomStateAtIndex(index).VolumeDown();
				}

				// Volume Release
				if (!sig.GetBoolValue() && (join == Joins.DIGITAL_ROOMS_VOLUME_UP || join == Joins.DIGITAL_ROOMS_VOLUME_DOWN))
				{
					roomGroupState.GetRoomStateAtIndex(index).VolumeStop();
				}

				// Mute toggle
				if (join == Joins.DIGITAL_ROOMS_MUTE && sig.GetBoolValue())
				{
					roomGroupState.GetRoomStateAtIndex(index).ToggleMute();
				}
			}

			// Room group
			if (sig.SmartObject == Joins.SMARTOBJECT_ROOM_GROUP && sig.Type == eSigType.Analog && sig.Number == Joins.DYNAMIC_BUTTON_LIST_DIGITAL_START_SELECTED_JOIN)
			{
				int index = sig.GetUShortValue();
				SetRoomGroup(crosspointState, index);
			}
		}

		#endregion

		public IEnumerable<SigInfo> GetSigCache()
		{
			m_SigCacheSection.Enter();
			try
			{
				return m_SigCache.ToList(m_SigCache.Count);
			}
			finally
			{
				m_SigCacheSection.Leave();
			}
		}

		private void SetInitialSigCacheValues()
		{
			var data = new CrosspointData();

			Update(data);

			m_SigCacheSection.Execute(() => m_SigCache.AddHighRemoveLow(data.GetSigs()));
		}

		private void SendInputData(CrosspointData data)
		{
			data.AddControlIds(Equipment.ControlCrosspoints);

			m_SigCacheSection.Execute(() => m_SigCache.AddHighRemoveLow(data.GetSigs()));

			Equipment.SendInputData(data);
		}


		#region Console
		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return "MultiRoomRouting" + m_Equipment.Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Multi Room Routing XP3 Interface"; } }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return m_Equipment;
			yield return ConsoleNodeGroup.KeyNodeMap("RoomGroupStates",m_RoomGroupStates.Values,s => (uint)s.Index);
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("XP3 Id", m_Equipment.Id);
			addRow("XP3 Name", m_Equipment.Name);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield break;
		}

		#endregion
	}
}
