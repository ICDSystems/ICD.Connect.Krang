#if SIMPLSHARP
using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Krang.Rooms;
using ICD.Connect.Krang.Routing.Endpoints.Sources;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.SPlusInterfaces
{
	[PublicAPI("S+")]
	public sealed class SPlusUiInterface : IDisposable
	{
		private const ushort INDEX_NOT_FOUND = 0;
		private const ushort INDEX_START = 1;
		private const ushort AUDIO_LIST_INDEX = 0;
		private const ushort VIDEO_LIST_INDEX = 1;

		public delegate void RoomInfoCallback(ushort id, SimplSharpString name, ushort index);

		public delegate void SourceInfoCallback(
			ushort id, SimplSharpString name, ushort crosspointId, ushort crosspointType, ushort sourceListIndex,
			ushort sourceIndex);

		public delegate void RoomListCallback(ushort index, ushort roomId, SimplSharpString roomName);

		public delegate void RoomListSizeCallback(ushort size);

		public delegate void SourceListCallback(
			ushort listIndex, ushort index, ushort sourceId, SimplSharpString sourceName, ushort crosspointId, ushort crosspointType);

		public delegate void SourceListSizeCallback(ushort listIndex, ushort size);

		#region Properties

		/// <summary>
		/// Raises the room info when the wrapped room changes.
		/// </summary>
		[PublicAPI]
		public RoomInfoCallback OnRoomChanged { get; set; }

		/// <summary>
		/// Raises for each source that is routed to the room destinations.
		/// </summary>
		[PublicAPI]
		public SourceInfoCallback OnSourceChanged { get; set; }

		/// <summary>
		/// Raised to update the rooms list for new rooms
		/// </summary>
		[PublicAPI]
		public RoomListCallback OnRoomListChanged { get; set; }

		[PublicAPI]
		public RoomListSizeCallback OnRoomListSizeChanged { get; set; }

		[PublicAPI]
		public SourceListCallback OnSourceListChanged { get; set; }

		[PublicAPI]
		public SourceListSizeCallback OnSourceListSizeChanged { get; set; }

		#endregion

		private SimplRoom m_Room;

		/// <summary>
		/// List of index to room
		/// </summary>
		private Dictionary<ushort, SimplRoom> m_RoomListDictionary;

		/// <summary>
		/// Reverse list of room to index
		/// </summary>
		private Dictionary<SimplRoom, ushort> m_RoomListDictionaryReverse;

		private Dictionary<ushort, Dictionary<ushort, SimplSource>> m_SourceListDictionary;

		private Dictionary<ISource, ushort[]> m_SourceListDictionaryReverse;

		/// <summary>
		/// Constructor.
		/// </summary>
		public SPlusUiInterface()
		{
			m_RoomListDictionary = new Dictionary<ushort, SimplRoom>();
			m_RoomListDictionaryReverse = new Dictionary<SimplRoom, ushort>();
			m_SourceListDictionary = new Dictionary<ushort, Dictionary<ushort, SimplSource>>();
			m_SourceListDictionaryReverse = new Dictionary<ISource, ushort[]>();

			try
			{
				SPlusKrangBootstrap.OnKrangLoaded += SPlusKrangBootstrapOnKrangLoaded;
			}
			catch (Exception e)
			{
				IcdErrorLog.Exception(e.GetBaseException(), "Failed to create Krang SPlusUiInterface - {0}", e.GetBaseException().Message);
			}
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnRoomChanged = null;
			OnSourceChanged = null;
			OnRoomListChanged = null;
			OnRoomListSizeChanged = null;
			OnSourceListChanged = null;
			OnSourceListSizeChanged = null;

			m_RoomListDictionary.Clear();
			m_RoomListDictionaryReverse.Clear();
			m_SourceListDictionary.Clear();
			m_SourceListDictionaryReverse.Clear();

			SPlusKrangBootstrap.OnKrangLoaded -= SPlusKrangBootstrapOnKrangLoaded;
		}

		/// <summary>
		/// Sets the current room for routing operations.
		/// </summary>
		/// <param name="roomId"></param>
		[PublicAPI("S+")]
		public void SetRoom(ushort roomId)
		{
			SimplRoom room = GetRoom(roomId);
			SetRoom(room);
		}

		/// <summary>
		/// Sets the current room, based on the room list index
		/// </summary>
		/// <param name="roomIndex">index of the room to set</param>
		[PublicAPI("S+")]
		public void SetRoomIndex(ushort roomIndex)
		{
			SimplRoom room;
			if (!m_RoomListDictionary.TryGetValue(roomIndex, out room))
				return;

			SetRoom(room);
		}

		/// <summary>
		/// Routes the source with the given id to all destinations in the current room.
		/// Unroutes if no source found with the given id.
		/// </summary>
		/// <param name="sourceId"></param>
		[PublicAPI("S+")]
		public void SetSource(ushort sourceId)
		{
			if (m_Room != null)
				m_Room.SetSource(sourceId);
		}

		[PublicAPI("S+")]
		public void SetSourceIndex(ushort sourceList, ushort sourceIndex)
		{
			SimplSource source;

			Dictionary<ushort, SimplSource> listDict;
			if (!m_SourceListDictionary.TryGetValue(sourceList, out listDict))
				return;

			if (!listDict.TryGetValue(sourceIndex, out source))
				return;

			SetSource((ushort)source.Id);
		}

		/// <summary>
		/// Called when the S+ class initializes, and calls all necessary delegates
		/// </summary>
		/// <param name="defaultRoom"></param>
		[PublicAPI("S+")]
		public void InitializeSPlus(ushort defaultRoom)
		{
			SetRoom(defaultRoom);
			RaiseRoomList();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sets the current room for routing operations.
		/// </summary>
		/// <param name="room"></param>
		private void SetRoom(SimplRoom room)
		{
			if (room == m_Room)
				return;

			Unsubscribe(m_Room);
			m_Room = room;
			Subscribe(m_Room);

			RaiseRoomInfo();
		}

		/// <summary>
		/// Gets the room for the given room id.
		/// </summary>
		/// <returns></returns>
		[CanBeNull]
		private static SimplRoom GetRoom(ushort id)
		{
			IOriginator output;
			SPlusKrangBootstrap.Krang.Originators.TryGetChild(id, out output);
			return output as SimplRoom;
		}

		private void RaiseRoomInfo()
		{
			RoomInfoCallback handler = OnRoomChanged;
			if (handler == null)
				return;

			if (m_Room == null)
			{
				handler(0, new SimplSharpString(string.Empty), INDEX_NOT_FOUND);
				return;
			}

			ushort index;

			if (!m_RoomListDictionaryReverse.TryGetValue(m_Room, out index))
				index = INDEX_NOT_FOUND;

			handler((ushort)m_Room.Id, new SimplSharpString(m_Room.Name ?? string.Empty), index);

			RaiseSourceList();
		}

		private void RaiseRoomList()
		{
			Dictionary<ushort, SimplRoom> roomListDictionary = new Dictionary<ushort, SimplRoom>();
			Dictionary<SimplRoom, ushort> roomListDictionaryReverse = new Dictionary<SimplRoom, ushort>();
			RoomListCallback handler = OnRoomListChanged;
			ushort i = INDEX_START;
			foreach (SimplRoom room in SPlusKrangBootstrap.Krang.Originators.GetChildren<SimplRoom>())
			{
				roomListDictionary[i] = room;
				roomListDictionaryReverse[room] = i;
				if (handler != null)
					handler(i,(ushort)room.Id, new SimplSharpString(room.Name));
				i++;
			}

			RoomListSizeCallback handlerListSize = OnRoomListSizeChanged;
			if (handlerListSize != null)
				handlerListSize((ushort)(i - 1));

			m_RoomListDictionary = roomListDictionary;
			m_RoomListDictionaryReverse = roomListDictionaryReverse;

			if (m_Room != null)
				RaiseRoomInfo();
		}

		private void RaiseSourceList()
		{
			Dictionary<ushort, Dictionary<ushort, SimplSource>> sourceListDictionary =
				new Dictionary<ushort, Dictionary<ushort,   SimplSource>>();
			sourceListDictionary[AUDIO_LIST_INDEX] = new Dictionary<ushort, SimplSource>();
			sourceListDictionary[VIDEO_LIST_INDEX] = new Dictionary<ushort, SimplSource>();
			Dictionary<ISource, ushort[]> sourceListDictionaryReverse = new Dictionary<ISource, ushort[]>();

			//ushort[] indexArray = {INDEX_START, INDEX_START};
			ushort audioListIndexCounter = INDEX_START;
			ushort videoListIndexCounter = INDEX_START;

			IEnumerable<SimplSource> sources =
				m_Room == null
					? Enumerable.Empty<SimplSource>()
					: m_Room.Originators.GetInstancesRecursive<SimplSource>();

			SourceListCallback handler = OnSourceListChanged;

			foreach (SimplSource ss in sources)
			{
				sourceListDictionaryReverse[ss] = new ushort[2];

				if (ss.SourceVisibility.HasFlag(SimplSource.eSourceVisibility.Audio))
				{
					sourceListDictionary[AUDIO_LIST_INDEX][audioListIndexCounter] = ss;
					sourceListDictionaryReverse[ss][AUDIO_LIST_INDEX] = audioListIndexCounter;
					if (handler != null)
						handler(AUDIO_LIST_INDEX, audioListIndexCounter, (ushort)ss.Id, new SimplSharpString(ss.Name), ss.CrosspointId, ss.CrosspointType);
					audioListIndexCounter++;
				}

				if (ss.SourceVisibility.HasFlag(SimplSource.eSourceVisibility.Video))
				{
					sourceListDictionary[VIDEO_LIST_INDEX][videoListIndexCounter] = ss;
					sourceListDictionaryReverse[ss][VIDEO_LIST_INDEX] = videoListIndexCounter;
					if (handler != null)
						handler(VIDEO_LIST_INDEX, videoListIndexCounter, (ushort)ss.Id, new SimplSharpString(ss.Name), ss.CrosspointId, ss.CrosspointType);
					videoListIndexCounter++;
				}

				SourceListSizeCallback handlerListSize = OnSourceListSizeChanged;
				if (handlerListSize != null)
				{
					handlerListSize(AUDIO_LIST_INDEX, (ushort)(audioListIndexCounter - 1));
					handlerListSize(VIDEO_LIST_INDEX, (ushort)(videoListIndexCounter - 1));
				}

			}

			m_SourceListDictionary = sourceListDictionary;
			m_SourceListDictionaryReverse = sourceListDictionaryReverse;
		}

		private void SPlusKrangBootstrapOnKrangLoaded(object sender, EventArgs eventArgs)
		{
			RaiseRoomList();
		}

		#endregion

		#region Room Callbacks

		/// <summary>
		/// Subscribe to the room events.
		/// </summary>
		/// <param name="room"></param>
		private void Subscribe(SimplRoom room)
		{
			if (room == null)
				return;
		}

		/// <summary>
		/// Unsubscribe from the room events.
		/// </summary>
		/// <param name="room"></param>
		private void Unsubscribe(SimplRoom room)
		{
			if (room == null)
				return;
		}

		#endregion
	}
}

#endif