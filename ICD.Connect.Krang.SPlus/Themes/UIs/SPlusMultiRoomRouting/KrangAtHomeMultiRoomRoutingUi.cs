using System.Collections.Generic;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting
{
	public sealed class KrangAtHomeMultiRoomRoutingUi : IKrangAtHomeUserInterface
	{
		private readonly Dictionary<int, AudioVideoEquipmentPage> m_Pages;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="theme"></param>
		public KrangAtHomeMultiRoomRoutingUi(KrangAtHomeTheme theme)
		{
			m_Pages = new Dictionary<int, AudioVideoEquipmentPage>();

			foreach (KeyValuePair<int, KrangAtHomeMultiRoomRouting> kvp in theme.MulitRoomRoutings)
			{
				var page = new AudioVideoEquipmentPage(theme, kvp.Value);
				m_Pages.Add(kvp.Key, page);
			}
		}

		public void Dispose()
		{
			foreach(KeyValuePair<int, AudioVideoEquipmentPage> kvp in m_Pages)
				kvp.Value.Dispose();
		}

		/// <summary>
		/// Updates the UI to represent the given room.
		/// </summary>
		/// <param name="room"></param>
		void IKrangAtHomeUserInterface.SetRoom(IKrangAtHomeRoom room)
		{
		}
	}
}