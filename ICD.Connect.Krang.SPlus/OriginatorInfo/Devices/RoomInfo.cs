using System;
using ICD.Connect.Krang.SPlus.Rooms;

namespace ICD.Connect.Krang.SPlus.OriginatorInfo.Devices
{
	[Serializable]
	public sealed class RoomInfo : AbstractOriginatorInfo
	{

		public RoomInfo(int id, string name) : base(id, name)
		{
			
		}

		public RoomInfo(IKrangAtHomeRoom room) : base(room)
		{
			
		}

		public RoomInfo()
		{
			
		}
	}
}