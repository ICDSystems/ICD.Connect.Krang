using ICD.Connect.Settings.Simpl;

namespace ICD.Connect.Krang.SPlus.SPlusRoomInfo.Device
{
	public interface ISPlusRoomInfo : ISimplOriginator
	{
		string RoomName { get; }
	}
}