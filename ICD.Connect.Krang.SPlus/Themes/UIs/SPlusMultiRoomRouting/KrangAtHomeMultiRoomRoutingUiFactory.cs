using System.Collections.Generic;
using ICD.Connect.Krang.SPlus.Rooms;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting
{
	public sealed class KrangAtHomeMultiRoomRoutingUiFactory : IKrangAtHomeUserInterfaceFactory
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
		public void Clear()
		{
			if (m_UserInterface != null)
				m_UserInterface.Dispose();
			m_UserInterface = null;
		}

		/// <summary>
		/// Instantiates the user interfaces for the originators in the core.
		/// </summary>
		/// <returns></returns>
		public void BuildUserInterfaces()
		{
			Clear();

			m_UserInterface = new KrangAtHomeMultiRoomRoutingUi(m_Theme);
		}

		/// <summary>
		/// Assigns the rooms to the existing user interfaces.
		/// </summary>
		void IKrangAtHomeUserInterfaceFactory.ReassignUserInterfaces()
		{
		}

		/// <summary>
		/// Assigns the rooms to the existing user interfaces.
		/// </summary>
		void IKrangAtHomeUserInterfaceFactory.AssignUserInterfaces(IEnumerable<IKrangAtHomeRoom> rooms)
		{
		}
	}
}
