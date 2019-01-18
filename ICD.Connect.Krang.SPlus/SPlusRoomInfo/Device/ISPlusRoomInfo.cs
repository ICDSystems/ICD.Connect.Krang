using ICD.Connect.Settings.Originators.Simpl;

namespace ICD.Connect.Krang.SPlus.SPlusRoomInfo.Device
{
	public interface ISPlusRoomInfo : ISimplOriginator
	{
		string RoomName { get; }
	}
}