using System;
using ICD.Connect.Krang.SPlus.Rooms;

namespace ICD.Connect.Krang.SPlus.Themes
{
	public interface IKrangAtHomeUserInterface : IDisposable
	{

		/// <summary>
		/// Updates the UI to represent the given room.
		/// </summary>
		/// <param name="room"></param>
		void SetRoom(IKrangAtHomeRoom room);
	}
}