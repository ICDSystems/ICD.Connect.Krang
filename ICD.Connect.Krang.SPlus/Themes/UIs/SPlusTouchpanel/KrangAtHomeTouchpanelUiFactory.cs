using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Device;
using ICD.Connect.Krang.SPlus.Rooms;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusTouchpanel
{
	public sealed class KrangAtHomeTouchpanelUiFactory : AbstractKrangAtHomeUiFactory<KrangAtHomeTouchpanelUi, KrangAtHomeSPlusTouchpanelDevice>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="theme"></param>
		public KrangAtHomeTouchpanelUiFactory(KrangAtHomeTheme theme) : base(theme)
		{
		}

		/// <summary>
		/// Instantiates the user interface for the given originator.
		/// </summary>
		/// <param name="originator"></param>
		/// <returns></returns>
		protected override KrangAtHomeTouchpanelUi CreateUserInterface(KrangAtHomeSPlusTouchpanelDevice originator)
		{
			return new KrangAtHomeTouchpanelUi(Theme, originator);
		}

		/// <summary>
		/// Returns true if the room contains the originator in the given ui.
		/// </summary>
		/// <param name="room"></param>
		/// <param name="ui"></param>
		/// <returns></returns>
		protected override bool RoomContainsOriginator(IKrangAtHomeRoom room, KrangAtHomeTouchpanelUi ui)
		{
			return room.Originators.ContainsRecursive(ui.Panel.Id);
		}
	}
}