#if SIMPLSHARP
using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Krang.Routing;
using ICD.Connect.Krang.Routing.Endpoints.Sources;
using ICD.Connect.Rooms;
using ICD.Connect.Rooms.Extensions;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Endpoints;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlusInterfaces
{
	[PublicAPI("S+")]
	public sealed class SPlusSimplRoom : IDisposable
	{
		public delegate void RoomInfoCallback(ushort id, SimplSharpString name);

		public delegate void SourceInfoCallback(ushort id, SimplSharpString name, ushort crosspointId, ushort crosspointType);

		/// <summary>
		/// Raises the room info when the wrapped room changes.
		/// </summary>
		public RoomInfoCallback OnRoomChanged { get; set; }

		/// <summary>
		/// Raises for each source that is routed to the room destinations.
		/// </summary>
		public SourceInfoCallback OnSourceChanged { get; set; }

		private ushort m_RoomId;
		private RoutingGraph m_SubscribedRoutingGraph;

		public SPlusSimplRoom()
		{
			SPlusKrangBootstrap.OnKrangLoaded += SPlusKrangBootstrapOnKrangLoaded;
			SPlusKrangBootstrap.OnKrangCleared += SPlusKrangBootstrapOnKrangCleared;

			Subscribe(SPlusKrangBootstrap.Krang.RoutingGraph);
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			SPlusKrangBootstrap.OnKrangLoaded -= SPlusKrangBootstrapOnKrangLoaded;
			SPlusKrangBootstrap.OnKrangCleared -= SPlusKrangBootstrapOnKrangCleared;

			Unsubscribe(SPlusKrangBootstrap.Krang.RoutingGraph);
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

		#endregion

		#region Routing

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
		/// Routes the source to all destinations in the current room.
		/// </summary>
		/// <param name="source"></param>
		private void Route(ISource source)
		{
			GetRoomDestinations().ForEach(d => Route(source, d));
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

		private void RaiseRoomInfo()
		{
			RoomInfoCallback handler = OnRoomChanged;
			if (handler == null)
				return;

			IRoom room = GetRoom();
			if (room == null)
				return;

			handler(m_RoomId, new SimplSharpString(room.Name ?? string.Empty));

			RoutingGraphOnRouteChanged(this, new EventArgs());
		}

		/// <summary>
		/// Gets the room for the current room id.
		/// </summary>
		/// <returns></returns>
		[CanBeNull]
		private IRoom GetRoom()
		{
			return SPlusKrangBootstrap.Krang.GetRooms().ContainsChild(m_RoomId)
				       ? SPlusKrangBootstrap.Krang.GetRooms()[m_RoomId]
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
			if (m_SubscribedRoutingGraph == null)
				return null;

			ISource source;
			m_SubscribedRoutingGraph.Sources.TryGetChild(id, out source);
			return source;
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

			if (m_SubscribedRoutingGraph == null)
				return Enumerable.Empty<ISource>();

			return m_SubscribedRoutingGraph.GetActiveSourceEndpoints(destination.Endpoint,
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
			if (m_SubscribedRoutingGraph == null)
				return null;

			IRoom room = GetRoom();
			return room == null
				       ? null
				       : m_SubscribedRoutingGraph.Sources.FirstOrDefault(s => s.Endpoint == endpoint);
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

		#region KrangBootstrap Callbacks

		private void SPlusKrangBootstrapOnKrangLoaded(object sender, EventArgs eventArgs)
		{
			Subscribe(SPlusKrangBootstrap.Krang.RoutingGraph);
			RaiseRoomInfo();
		}

		private void SPlusKrangBootstrapOnKrangCleared(object sender, EventArgs e)
		{
			Unsubscribe(m_SubscribedRoutingGraph);
		}

		#endregion

		#region RoutingGraph Callbacks

		private void Subscribe(RoutingGraph routingGraph)
		{
			if (routingGraph == null)
				return;

			routingGraph.OnRouteChanged += RoutingGraphOnRouteChanged;
		}

		private void Unsubscribe(RoutingGraph routingGraph)
		{
			if (routingGraph == null)
				return;

			routingGraph.OnRouteChanged -= RoutingGraphOnRouteChanged;
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

		#endregion
	}
}

#endif
