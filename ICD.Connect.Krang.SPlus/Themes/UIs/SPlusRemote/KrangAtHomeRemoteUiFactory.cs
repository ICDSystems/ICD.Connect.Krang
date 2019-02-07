using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Device;
using ICD.Connect.Krang.SPlus.Rooms;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusRemote
{
	public sealed class KrangAtHomeRemoteUiFactory : AbstractKrangAtHomeUiFactory<KrangAtHomeRemoteUi, KrangAtHomeSPlusRemoteDevice>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="theme"></param>
		public KrangAtHomeRemoteUiFactory(KrangAtHomeTheme theme) : base(theme)
		{
		}

		/// <summary>
		/// Instantiates the user interface for the given originator.
		/// </summary>
		/// <param name="originator"></param>
		/// <returns></returns>
		protected override KrangAtHomeRemoteUi CreateUserInterface(KrangAtHomeSPlusRemoteDevice originator)
		{
			return new KrangAtHomeRemoteUi(Theme, originator);
		}

		/// <summary>
		/// Returns true if the room contains the originator in the given ui.
		/// </summary>
		/// <param name="room"></param>
		/// <param name="ui"></param>
		/// <returns></returns>
		protected override bool RoomContainsOriginator(IKrangAtHomeRoom room, KrangAtHomeRemoteUi ui)
		{
			return room.Originators.ContainsRecursive(ui.UiDevice.Id);
		}
	}
}