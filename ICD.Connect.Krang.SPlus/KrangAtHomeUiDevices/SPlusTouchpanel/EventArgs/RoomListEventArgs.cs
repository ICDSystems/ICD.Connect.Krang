using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public sealed class RoomListEventArgs : GenericEventArgs<List<RoomInfo>>
	{
		public RoomListEventArgs(List<RoomInfo> roomList)
			: base(roomList)
		{
		}
	}
}