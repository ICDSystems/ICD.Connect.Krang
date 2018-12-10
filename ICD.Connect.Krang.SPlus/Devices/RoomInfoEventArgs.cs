using ICD.Common.Utils.EventArguments;
using ICD.Connect.Krang.SPlus.Rooms;

namespace ICD.Connect.Krang.SPlus.Devices
{
	public class RoomInfoEventArgs : GenericEventArgs<IKrangAtHomeRoom>
	{

		public ushort Index { get; private set; }

		public RoomInfoEventArgs(IKrangAtHomeRoom room, ushort index) : base(room)
		{
			Index = index;
		}

	}
}