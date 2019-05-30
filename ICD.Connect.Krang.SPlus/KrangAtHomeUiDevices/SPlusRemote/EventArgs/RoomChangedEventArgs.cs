using ICD.Common.Utils.EventArguments;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.Rooms;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.EventArgs
{
	public sealed class RoomChangedEventArgs : GenericEventArgs<RoomInfo>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public RoomChangedEventArgs(RoomInfo data) : base(data)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="room"></param>
		public RoomChangedEventArgs(IKrangAtHomeRoom room) : base(new RoomInfo(room))
		{
		}
	}
}