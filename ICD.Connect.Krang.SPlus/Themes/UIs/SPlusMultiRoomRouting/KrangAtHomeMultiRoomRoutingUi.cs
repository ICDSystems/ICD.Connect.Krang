using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting
{
	public sealed class KrangAtHomeMultiRoomRoutingUi : IKrangAtHomeUserInterface
	{
		private readonly AudioEquipmentPage m_AudioPage;
		private readonly VideoEquipmentPage m_VideoPage;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="theme"></param>
		public KrangAtHomeMultiRoomRoutingUi(KrangAtHomeTheme theme)
		{
			if (theme.AudioEquipment != null)
				m_AudioPage = new AudioEquipmentPage(theme.AudioEquipment);

			if (theme.VideoEquipment != null)
				m_VideoPage = new VideoEquipmentPage(theme.VideoEquipment);
		}

		public void Dispose()
		{
			m_AudioPage.Dispose();
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