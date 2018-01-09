#if SIMPLSHARP
using System;
using Crestron.SimplSharp;
using ICD.Common.Properties;
using ICD.Connect.Krang.Rooms;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.SPlusInterfaces
{
	[PublicAPI("S+")]
	public sealed class SPlusSimplRoom : IDisposable
	{
		public delegate void RoomInfoCallback(ushort id, SimplSharpString name);

		public delegate void SourceInfoCallback(ushort id, SimplSharpString name, ushort crosspointId, ushort crosspointType);

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

		#endregion

		private SimplRoom m_Room;

		/// <summary>
		/// Constructor.
		/// </summary>
		public SPlusSimplRoom()
		{
			SPlusKrangBootstrap.OnKrangLoaded += SPlusKrangBootstrapOnKrangLoaded;
			//SPlusKrangBootstrap.OnKrangCleared += SPlusKrangBootstrapOnKrangCleared;
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			SPlusKrangBootstrap.OnKrangLoaded -= SPlusKrangBootstrapOnKrangLoaded;
			//SPlusKrangBootstrap.OnKrangCleared -= SPlusKrangBootstrapOnKrangCleared;

			SetRoom(null);
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

		private void RaiseRoomInfo()
		{
			RoomInfoCallback handler = OnRoomChanged;
			if (handler == null)
				return;

			if (m_Room == null)
				return;

			handler((ushort)m_Room.Id, new SimplSharpString(m_Room.Name ?? string.Empty));

			//RoutingGraphOnRouteChanged(this, new EventArgs());
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

		#endregion

		#region KrangBootstrap Callbacks

		/// <summary>
		/// Called when the core finishes loading settings.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void SPlusKrangBootstrapOnKrangLoaded(object sender, EventArgs eventArgs)
		{
			RaiseRoomInfo();
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
