using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Partitioning.Rooms;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	[Serializable]
	public sealed class RoomSelected
	{
		public int Index { get; set; }
		public RoomInfo RoomInfo { get; set; }

		public RoomSelected(RoomInfo roomInfo, int index)
		{
			RoomInfo = roomInfo;
			Index = index;
		}

		public RoomSelected(IKrangAtHomeRoom room, int index)
		{
			RoomInfo = new RoomInfo(room);
			Index = index;
		}

		public RoomSelected(int index)
		{
			Index = index;
		}

		public RoomSelected()
		{
		}
	}

	public sealed class RoomSelectedEventArgs : GenericEventArgs<RoomSelected>
	{

		public int Index { get { return Data.Index; }}

		public RoomInfo RoomInfo {get { return Data.RoomInfo; }}

		public RoomSelectedEventArgs(RoomSelected roomSelected)
			: base(roomSelected)
		{
			
		}

		public RoomSelectedEventArgs(RoomInfo roomInfo, int index)
			: base(new RoomSelected()
			{
				Index = index,
				RoomInfo = roomInfo
			})
		{
		}

		public RoomSelectedEventArgs(IKrangAtHomeRoom room, int index)
			: base(new RoomSelected()
			{
				Index = index,
				RoomInfo = new RoomInfo(room)
			})
		{
		}
	}
}