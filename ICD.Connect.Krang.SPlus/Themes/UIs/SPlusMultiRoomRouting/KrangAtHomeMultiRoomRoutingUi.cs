using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages;
using ICD.Connect.Routing.Connections;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting
{
	public sealed class KrangAtHomeMultiRoomRoutingUi : IKrangAtHomeUserInterface
	{
		private readonly AudioVideoEquipmentPage m_AudioPage;
		private readonly AudioVideoEquipmentPage m_VideoPage;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="theme"></param>
		public KrangAtHomeMultiRoomRoutingUi(KrangAtHomeTheme theme)
		{
			if (theme.AudioEquipment != null)
				m_AudioPage = new AudioVideoEquipmentPage(theme, theme.AudioEquipment, eConnectionType.Audio);

			if (theme.VideoEquipment != null)
				m_VideoPage = new AudioVideoEquipmentPage(theme, theme.VideoEquipment, eConnectionType.Video);
		}

		public void Dispose()
		{
			if (m_AudioPage != null)
				m_AudioPage.Dispose();
			if (m_VideoPage != null)
				m_VideoPage.Dispose();
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