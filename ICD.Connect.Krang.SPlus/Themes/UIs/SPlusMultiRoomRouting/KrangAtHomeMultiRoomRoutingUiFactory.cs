using System.Collections.Generic;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Themes.UserInterfaceFactories;
using ICD.Connect.Themes.UserInterfaces;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting
{
	public sealed class KrangAtHomeMultiRoomRoutingUiFactory : AbstractUserInterfaceFactory, IKrangAtHomeUserInterfaceFactory
	{
		private readonly KrangAtHomeTheme m_Theme;

		private KrangAtHomeMultiRoomRoutingUi m_UserInterface;

		public KrangAtHomeMultiRoomRoutingUi UserInterface { get { return m_UserInterface; }}

		public KrangAtHomeMultiRoomRoutingUiFactory(KrangAtHomeTheme krangAtHomeTheme)
		{
			m_Theme = krangAtHomeTheme;
		}

		/// <summary>
		/// Disposes the instantiated UIs.
		/// </summary>
		public override void Clear()
		{
			if (m_UserInterface != null)
				m_UserInterface.Dispose();
			m_UserInterface = null;
		}

		/// <summary>
		/// Instantiates the user interfaces for the originators in the core.
		/// </summary>
		/// <returns></returns>
		public override void BuildUserInterfaces()
		{
			Clear();

			m_UserInterface = new KrangAtHomeMultiRoomRoutingUi(m_Theme);
		}

		/// <summary>
		/// Gets the instantiated user interfaces.
		/// </summary>
		public override IEnumerable<IUserInterface> GetUserInterfaces()
		{
			yield return m_UserInterface;
		}

		/// <summary>
		/// Assigns the rooms to the existing user interfaces.
		/// </summary>
		public override void ReassignUserInterfaces()
		{
		}

		/// <summary>
		/// Assigns the rooms to the existing user interfaces.
		/// </summary>
		public override void AssignUserInterfaces(IEnumerable<IRoom> rooms)
		{
		}

		/// <summary>
		/// Activates this user interface.
		/// </summary>
		public override void ActivateUserInterfaces()
		{
		}
	}
}
