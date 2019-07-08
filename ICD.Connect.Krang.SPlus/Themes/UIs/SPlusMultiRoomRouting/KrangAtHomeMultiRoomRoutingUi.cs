using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages;
using ICD.Connect.Partitioning.Rooms;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting
{
	public sealed class KrangAtHomeMultiRoomRoutingUi : IKrangAtHomeUserInterface, IConsoleNode
	{
		private readonly Dictionary<int, AudioVideoEquipmentPage> m_Pages;

		/// <summary>
		/// Gets the room attached to this UI.
		/// </summary>
		public IRoom Room { get { return null; } }

		/// <summary>
		/// Gets the target instance attached to this UI (i.e. the Panel, KeyPad, etc).
		/// </summary>
		public object Target { get { return null; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="theme"></param>
		public KrangAtHomeMultiRoomRoutingUi(KrangAtHomeTheme theme)
		{
			m_Pages = new Dictionary<int, AudioVideoEquipmentPage>();

			foreach (KeyValuePair<int, KrangAtHomeMultiRoomRouting> kvp in theme.MultiRoomRoutings)
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
		/// Tells the UI that it should be considered ready to use.
		/// For example updating the online join on a panel or starting a long-running process that should be delayed.
		/// </summary>
		public void Activate()
		{
		}

		/// <summary>
		/// Updates the UI to represent the given room.
		/// </summary>
		/// <param name="room"></param>
		public void SetRoom(IRoom room)
		{
		}

		/// <summary>
		/// Updates the UI to represent the given room.
		/// </summary>
		/// <param name="room"></param>
		void IKrangAtHomeUserInterface.SetRoom(IKrangAtHomeRoom room)
		{
		}


		#region Console
		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return "MultiRoomRoutingUi"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Krang at Home MultiRoom Routing UI"; } }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return ConsoleNodeGroup.KeyNodeMap("AudioVideoPages", m_Pages.Values, p => (uint)p.Equipment.Id);
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield break;
		}

		#endregion
	}
}