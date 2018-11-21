using System;
using ICD.Common.Properties;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Routing.Endpoints;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Settings.SPlusShims;
#if SIMPLSHARP
using ICDPlatformString = Crestron.SimplSharp.SimplSharpString;
#else
using ICDPlatformString = System.String;
#endif

namespace ICD.Connect.Krang.SPlus.SPlusShims
{
	[PublicAPI("S+")]
	public sealed class SPlusSimplRoomShim : AbstractSPlusOriginatorShim<SimplRoom>
	{
		public delegate void RoomInfoCallback(ushort id, ICDPlatformString name);

		public delegate void SourceInfoCallback(ushort id, ICDPlatformString name, ushort crosspointId, ushort crosspointType);

		#region Properties

		/// <summary>
		/// Raises the room info when the wrapped room changes.
		/// </summary>
		[PublicAPI]
		public RoomInfoCallback RoomChanged { get; set; }

		/// <summary>
		/// Raises for each source that is routed to the room destinations.
		/// </summary>
		[PublicAPI]
		public SourceInfoCallback SourceChanged { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();
			RoomChanged = null;
			SourceChanged = null;
		}

		/// <summary>
		/// Routes the source with the given id to all destinations in the current room.
		/// Unroutes if no source found with the given id.
		/// </summary>
		/// <param name="sourceId"></param>
		[PublicAPI("S+")]
		public void SetSource(ushort sourceId)
		{
			if (Originator != null)
				Originator.SetSource(sourceId);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Called when the originator is attached.
		/// Do any actions needed to syncronize
		/// </summary>
		protected override void InitializeOriginator()
		{
			base.InitializeOriginator();
			RaiseRoomInfo();
			RaiseSourceInfo();
		}

		private void RaiseRoomInfo()
		{
			RoomInfoCallback handler = RoomChanged;
			if (handler == null)
				return;

			if (Originator == null)
				return;

			handler((ushort)Originator.Id, Originator.Name ?? string.Empty);

			// Hack to raise the current routed source as well
			RoomOnActiveSourcesChange(null, EventArgs.Empty);
		}

		private void RaiseSourceInfo()
		{
			SourceInfoCallback handler = SourceChanged;
			if (handler == null)
				return;

			ISource source = Originator == null ? null : Originator.GetSource();

			ushort id = source == null ? (ushort)0 : (ushort)source.Id;
			string name = source == null
							  ? string.Empty
							  : source.GetNameOrDeviceName();
			ushort crosspointId = source is SimplSource ? (source as SimplSource).CrosspointId : (ushort)0;
			ushort crosspointType = source is SimplSource ? (source as SimplSource).CrosspointType : (ushort)0;

			handler(id, name, crosspointId, crosspointType);
		}

		#endregion

		#region Room Callbacks

		/// <summary>
		/// Subscribe to the room events.
		/// </summary>
		/// <param name="room"></param>
		protected override void Subscribe(SimplRoom room)
		{
			base.Subscribe(room);

			if (room != null)
				room.OnActiveSourcesChange += RoomOnActiveSourcesChange;
		}

		/// <summary>
		/// Unsubscribe from the room events.
		/// </summary>
		/// <param name="room"></param>
		protected override void Unsubscribe(SimplRoom room)
		{
			base.Unsubscribe(room);

			if (room != null)
				room.OnActiveSourcesChange -= RoomOnActiveSourcesChange;
		}

		/// <summary>
		/// Raised when source/s become actively/inactively routed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void RoomOnActiveSourcesChange(object sender, EventArgs eventArgs)
		{
			RaiseSourceInfo();
		}

		#endregion
	}
}
