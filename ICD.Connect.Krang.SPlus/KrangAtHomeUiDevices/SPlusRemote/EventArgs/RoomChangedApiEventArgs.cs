using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Proxy;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.Rooms;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.EventArgs
{
	public sealed class RoomChangedApiEventArgs : AbstractGenericApiEventArgs<RoomInfo>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public RoomChangedApiEventArgs(RoomInfo data) : base(SPlusRemoteApi.EVENT_ROOM_CHANGED, data)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="room"></param>
		public RoomChangedApiEventArgs(IKrangAtHomeRoom room) : base(SPlusRemoteApi.EVENT_ROOM_CHANGED, new RoomInfo(room))
		{
		}
	}
}