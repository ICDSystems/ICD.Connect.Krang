using System.Collections.Generic;
using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.Proxy;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.EventArgs
{
	public sealed class RoomListEventArgs : AbstractGenericApiEventArgs<IEnumerable<KeyValuePair<ushort, RoomInfo>>>
	{
		public RoomListEventArgs(IEnumerable<KeyValuePair<ushort, RoomInfo>> roomList)
			: base(SPlusTouchpanelDeviceApi.EVENT_ROOM_LIST, roomList)
		{
		}
	}
}