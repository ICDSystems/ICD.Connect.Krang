using System;
using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.Proxy;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.EventArgs
{
	[Serializable]
	public sealed class RoomSelected
	{
		public ushort Index { get; set; }
		public RoomInfo RoomInfo { get; set; }
	}

	public sealed class RoomSelectedEventArgs : AbstractGenericApiEventArgs<RoomSelected>
	{

		public ushort Index { get { return Data.Index; }}

		public RoomInfo RoomInfo {get { return Data.RoomInfo; }}

		public RoomSelectedEventArgs(RoomSelected roomSelected)
			: base(SPlusTouchpanelDeviceApi.EVENT_ROOM_SELECTED, roomSelected)
		{
			
		}

		public RoomSelectedEventArgs(RoomInfo roomInfo, ushort index)
			: base(SPlusTouchpanelDeviceApi.EVENT_ROOM_SELECTED, new RoomSelected()
			{
				Index = index,
				RoomInfo = roomInfo
			})
		{
		}

		public RoomSelectedEventArgs(IKrangAtHomeRoom room, ushort index)
			: base(SPlusTouchpanelDeviceApi.EVENT_ROOM_SELECTED, new RoomSelected()
			{
				Index = index,
				RoomInfo = new RoomInfo(room)
			})
		{
		}
	}
}