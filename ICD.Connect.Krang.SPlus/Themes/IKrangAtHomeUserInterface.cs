using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Themes.UserInterfaces;

namespace ICD.Connect.Krang.SPlus.Themes
{
	public interface IKrangAtHomeUserInterface : IUserInterface
	{
		/// <summary>
		/// Updates the UI to represent the given room.
		/// </summary>
		/// <param name="room"></param>
		void SetRoom(IKrangAtHomeRoom room);
	}
}
