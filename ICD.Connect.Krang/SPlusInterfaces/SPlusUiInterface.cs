using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Interfaces.Rooms;
using ICD.Connect.Interfaces.Routing.Connections;
using ICD.Connect.Interfaces.Routing.Endpoints;
using ICD.Connect.Interfaces.Routing.Endpoints.Destinations;
using ICD.Connect.Interfaces.Routing.Endpoints.Sources;
using ICD.Connect.Krang.Routing.Endpoints.Sources;

#if SIMPLSHARP

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

		public delegate void SourceInfoCallback(ushort id, SimplSharpString name, ushort crosspointId, ushort crosspointType);

		public delegate void RoomListCallback(ushort index, ushort roomId, SimplSharpString roomName);

		public delegate void SourceListCallback(
			ushort index, ushort sourceId, SimplSharpString sourceName, ushort crosspointId, ushort crosspointType);

		/// <summary>
		/// Raises the room info when the wrapped room changes.
		/// </summary>
		public event RoomInfoCallback OnRoomChanged;

	/// <summary>
		/// Raises for each source that is routed to the room destinations.
		/// </summary>
		public event SourceInfoCallback OnSourceChanged;

		/// <summary>
		/// Raised to update the rooms list for new rooms
		/// </summary>
		public event RoomListCallback OnRoomListChanged;

		public event SourceListCallback OnSourceListChanged;

		private ushort m_RoomId;

		/// <summary>
		/// List of index to room
		/// </summary>
		private Dictionary<ushort, IRoom> m_RoomListDictionary;

		/// <summary>
		/// Reverse list of room to index
		/// </summary>
		private Dictionary<IRoom, ushort> m_RoomListDictionaryReverse;

		private Dictionary<ushort, Dictionary<ushort, SimplSource>> m_SourceListDictionary;

		private Dictionary<ISource, ushort[]> m_SourceListDictionaryReverse;

		public SPlusUiInterface()
		{
			SPlusKrangBootstrap.OnKrangLoaded += SPlusKrangBootstrapOnKrangLoaded;
			SPlusKrangBootstrap.Krang.RoutingGraph.OnRouteChanged += RoutingGraphOnRouteChanged;
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			SPlusKrangBootstrap.OnKrangLoaded -= SPlusKrangBootstrapOnKrangLoaded;
			SPlusKrangBootstrap.Krang.RoutingGraph.OnRouteChanged -= RoutingGraphOnRouteChanged;
		}

		/// <summary>
		/// Sets the current room for routing operations.
		/// </summary>
		/// <param name="roomId"></param>
		[PublicAPI("S+")]
		public void SetRoom(ushort roomId)
		{
			if (roomId == m_RoomId)
				return;

			m_RoomId = roomId;

			RaiseRoomInfo();
		}

		/// <summary>
		/// Sets the current room, based on the room list index
		/// </summary>
		/// <param name="roomIndex">index of the room to set</param>
		[PublicAPI("S+")]
		public void SetRoomIndex(ushort roomIndex)
		{
			IRoom room;

			if (!m_RoomListDictionary.TryGetValue(roomIndex, out room))
				return;

			m_RoomId = (ushort)room.Id;

			RaiseRoomInfo();
		}

		/// <summary>
		/// Routes the source with the given id to all destinations in the current room.
		/// Unroutes if no source found with the given id.
		/// </summary>
		/// <param name="sourceId"></param>
		[PublicAPI("S+")]
		public void SetSource(ushort sourceId)
		{
			ISource source = GetSource(sourceId);
			if (source == null)
				Unroute();
			else
				Route(source);
		}

		/// <summary>
		/// Called when the S+ class initializes, and calls all necessary delegates
		/// </summary>
		/// <param name="defaultRoom"></param>
		public void InitializeSPlus(ushort defaultRoom)
		{
			SetRoom(defaultRoom);
			RaiseRoomList();
		}

		#endregion

		#region Routing


		private void RouteSourceIndex(ushort sourceList, ushort sourceIndex)
		{
			Dictionary<ushort, SimplSource> sourceListLayerOne;
			if (!m_SourceListDictionary.TryGetValue(sourceList, out sourceListLayerOne))
				return;
			SimplSource source;
			if (!sourceListLayerOne.TryGetValue(sourceIndex, out source))
				return;

			eConnectionType connectionType;
			switch (sourceList)
			{
				case AUDIO_LIST_INDEX:
					connectionType = eConnectionType.Audio;
					break;
				case VIDEO_LIST_INDEX:
					connectionType = eConnectionType.Audio | eConnectionType.Video;
					break;
				default:
					return;
			}

			Route(source, connectionType);
		}

		/// <summary>
		/// Routes the source to the destination.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		private void Route(ISource source, IDestination destination)
		{
			eConnectionType connectionType = EnumUtils.GetFlagsIntersection(source.ConnectionType, destination.ConnectionType);
			SPlusKrangBootstrap.Krang.RoutingGraph.Route(source.Endpoint, destination.Endpoint, connectionType, m_RoomId);
		}

		/// <summary>
		/// Routes the source to the destination.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		/// <param name="connectionType"></param>
		private void Route(ISource source, IDestination destination, eConnectionType connectionType)
		{
			//route
			eConnectionType routeConnectionType = EnumUtils.GetFlagsIntersection(source.ConnectionType,
			                                                                     destination.ConnectionType, connectionType);
			SPlusKrangBootstrap.Krang.RoutingGraph.Route(source.Endpoint, destination.Endpoint, routeConnectionType, m_RoomId);

			//unroute
			eConnectionType unrouteConnectionType = destination.ConnectionType & ~connectionType;
			SPlusKrangBootstrap.Krang.RoutingGraph.Unroute(source.Endpoint, destination.Endpoint, unrouteConnectionType, m_RoomId);


		}

		/// <summary>
		/// Routes the source to all destinations in the current room.
		/// </summary>
		/// <param name="source"></param>
		private void Route(ISource source)
		{
			GetRoomDestinations().ForEach(d => Route(source, d));
		}

		/// <summary>
		/// Routes the source to all destinations in the current room.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="connectionType"></param>
		private void Route(ISource source, eConnectionType connectionType)
		{
			GetRoomDestinations().ForEach(d => Route(source, d, connectionType));
		}

		/// <summary>
		/// Unroutes the source from the destination.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		private void Unroute(ISource source, IDestination destination)
		{
			eConnectionType connectionType = EnumUtils.GetFlagsIntersection(source.ConnectionType, destination.ConnectionType);
			SPlusKrangBootstrap.Krang.RoutingGraph.Unroute(source.Endpoint, destination.Endpoint, connectionType, m_RoomId);
		}

		/// <summary>
		/// Unroutes all sources from the destination.
		/// </summary>
		/// <param name="destination"></param>
		private void Unroute(IDestination destination)
		{
			GetActiveSources(destination).ForEach(s => Unroute(s, destination));
		}

		/// <summary>
		/// Unroutes all the destinations in the room.
		/// </summary>
		private void Unroute()
		{
			GetRoomDestinations().ForEach(Unroute);
		}

		#endregion

		#region Private Methods

		private void SPlusKrangBootstrapOnKrangLoaded(object sender, EventArgs eventArgs)
		{
			RaiseRoomList();
		}

		private void RoutingGraphOnRouteChanged(object sender, EventArgs eventArgs)
		{
			SourceInfoCallback handler = OnSourceChanged;
			if (handler == null)
				return;

			foreach (ISource source in GetActiveRoomSources())
			{
				IRoom room = GetRoom();

				ushort id = source == null ? (ushort)0 : (ushort)source.Id;
				string name = source == null
					              ? string.Empty
					              : room == null
						                ? source.Name
						                : source.GetNameOrDeviceName(room);
				ushort crosspointId = source is SimplSource ? (source as SimplSource).CrosspointId : (ushort)0;
				ushort crosspointType = source is SimplSource ? (source as SimplSource).CrosspointType : (ushort)0;

				handler(id, new SimplSharpString(name), crosspointId, crosspointType);
			}
		}

		private void RaiseRoomInfo()
		{
			RoomInfoCallback handler = OnRoomChanged;
			if (handler == null)
				return;

			IRoom room = GetRoom();
			if (room == null)
			{
				handler(0, new SimplSharpString(""), INDEX_NOT_FOUND);
				return;
			}

			ushort index;

			if (!m_RoomListDictionaryReverse.TryGetValue(room, out index))
				index = INDEX_NOT_FOUND;

			handler(m_RoomId, new SimplSharpString(room.Name ?? string.Empty), index);

			RaiseSourceList();
		}

		private void RaiseRoomList()
		{
			var roomListDictionary = new Dictionary<ushort, IRoom>();
			var roomListDictionaryReverse = new Dictionary<IRoom, ushort>();
			var handler = OnRoomListChanged;
			ushort i = INDEX_START;
			foreach (var room in SPlusKrangBootstrap.Krang.Rooms)
			{
				roomListDictionary[i] = room;
				roomListDictionaryReverse[room] = i;
				if (handler != null)
					handler(i,(ushort)room.Id, new SimplSharpString(room.Name));
				i++;
			}

			m_RoomListDictionary = roomListDictionary;
			m_RoomListDictionaryReverse = roomListDictionaryReverse;

			if (m_RoomId != 0)
				RaiseRoomInfo();
		}

		private void RaiseSourceList()
		{
			var sourceListDictionary = new Dictionary<ushort, Dictionary<ushort,   SimplSource>>();
			var sourceListDictionaryReverse = new Dictionary<ISource, ushort[]>();

			ushort[] indexArray = new ushort[] {INDEX_START, INDEX_START};

			var sources = GetRoom().Sources.ToList();

			var handler = OnSourceListChanged;

			foreach (var ss in sources.OfType<SimplSource>())
			{
				if (ss.SourceVisibility.HasFlag(eConnectionType.Audio))
				{
					sourceListDictionary[AUDIO_LIST_INDEX][indexArray[AUDIO_LIST_INDEX]] = ss;
					sourceListDictionaryReverse[ss][AUDIO_LIST_INDEX] = indexArray[AUDIO_LIST_INDEX];
					if (handler != null)
						handler(indexArray[AUDIO_LIST_INDEX], (ushort)ss.Id, new SimplSharpString(ss.Name), ss.CrosspointId, ss.CrosspointType);
					indexArray[AUDIO_LIST_INDEX]++;
				}
				if (ss.SourceVisibility.HasFlag(eConnectionType.Video))
				{
					sourceListDictionary[VIDEO_LIST_INDEX][indexArray[VIDEO_LIST_INDEX]] = ss;
					sourceListDictionaryReverse[ss][VIDEO_LIST_INDEX] = indexArray[VIDEO_LIST_INDEX];
					if (handler != null)
						handler(indexArray[VIDEO_LIST_INDEX], (ushort)ss.Id, new SimplSharpString(ss.Name), ss.CrosspointId, ss.CrosspointType);
					indexArray[VIDEO_LIST_INDEX]++;
				}
			}

		}

		/// <summary>
		/// Gets the room for the current room id.
		/// </summary>
		/// <returns></returns>
		[CanBeNull]
		private IRoom GetRoom()
		{
			return SPlusKrangBootstrap.Krang.Rooms.ContainsChild(m_RoomId)
				       ? SPlusKrangBootstrap.Krang.Rooms[m_RoomId]
				       : null;
		}

		/// <summary>
		/// Gets the source with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[CanBeNull]
		private ISource GetSource(ushort id)
		{
			return SPlusKrangBootstrap.Krang.RoutingGraph.Sources.ContainsChild(id)
				       ? SPlusKrangBootstrap.Krang.RoutingGraph.Sources[m_RoomId]
				       : null;
		}

		/// <summary>
		/// Gets the sources currently routed to the room destinations.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<ISource> GetActiveRoomSources()
		{
			return GetRoomDestinations().SelectMany(d => GetActiveSources(d));
		}

		/// <summary>
		/// Gets the sources currently routed to the given destination.
		/// </summary>
		/// <param name="destination"></param>
		/// <returns></returns>
		private IEnumerable<ISource> GetActiveSources(IDestination destination)
		{
			if (destination == null)
				throw new ArgumentNullException("destination");

			return SPlusKrangBootstrap.Krang.RoutingGraph.GetActiveSourceEndpoints(destination.Endpoint,
			                                                                       destination.ConnectionType, false)
			                          .Select(e => GetSourceFromEndpoint(e))
			                          .Where(s => s != null);
		}

		/// <summary>
		/// Gets the source for the given endpoint info.
		/// </summary>
		/// <param name="endpoint"></param>
		/// <returns></returns>
		[CanBeNull]
		private ISource GetSourceFromEndpoint(EndpointInfo endpoint)
		{
			IRoom room = GetRoom();
			return room == null
				       ? null
				       : SPlusKrangBootstrap.Krang.RoutingGraph.Sources.FirstOrDefault(s => s.Endpoint == endpoint);
		}

		/// <summary>
		/// Gets the destinations for the current room.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IDestination> GetRoomDestinations()
		{
			IRoom room = GetRoom();
			return room == null ? Enumerable.Empty<IDestination>() : room.Destinations;
		}

		#endregion
	}
}

#endif