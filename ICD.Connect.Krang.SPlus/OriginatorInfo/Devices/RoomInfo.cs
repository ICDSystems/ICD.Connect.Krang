using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Connect.Krang.SPlus.Rooms;

namespace ICD.Connect.Krang.SPlus.OriginatorInfo.Devices
{
	[Serializable]
	public sealed class RoomInfo : AbstractOriginatorInfo
	{

		public KeyValuePair<eCrosspointType, ushort>[] Crosspoints { get; set; }

		public RoomInfo(int id, string name) : base(id, name)
		{
			
		}

		public RoomInfo(IKrangAtHomeRoom room) : base(room)
		{
			if (room != null)
				Crosspoints = room.GetCrosspoints().ToArray();
		}

		public RoomInfo()
		{
			
		}
	}
}