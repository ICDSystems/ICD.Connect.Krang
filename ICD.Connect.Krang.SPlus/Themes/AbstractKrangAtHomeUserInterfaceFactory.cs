using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Settings.Originators;
using ICD.Connect.Themes.UserInterfaceFactories;
using ICD.Connect.Themes.UserInterfaces;

namespace ICD.Connect.Krang.SPlus.Themes
{
	public abstract class AbstractKrangAtHomeUiFactory<TUserInterface, TOriginator> : AbstractUserInterfaceFactory, IKrangAtHomeUserInterfaceFactory
		where TUserInterface : IKrangAtHomeUserInterface
		where TOriginator : IOriginator
	{
		private readonly KrangAtHomeTheme m_Theme;

		private readonly IcdHashSet<TUserInterface> m_UserInterfaces;
		private readonly SafeCriticalSection m_UserInterfacesSection;

		protected KrangAtHomeTheme Theme { get { return m_Theme; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="theme"></param>
		protected AbstractKrangAtHomeUiFactory(KrangAtHomeTheme theme)
		{
			m_Theme = theme;

			m_UserInterfaces = new IcdHashSet<TUserInterface>();
			m_UserInterfacesSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Disposes the instantiated UIs.
		/// </summary>
		public override void Clear()
		{
			m_UserInterfacesSection.Enter();

			try
			{
				m_UserInterfaces.ForEach(ui => ui.Dispose());
				m_UserInterfaces.Clear();
			}
			finally
			{
				m_UserInterfacesSection.Leave();
			}
		}

		/// <summary>
		/// Instantiates the user interfaces for the originators in the core.
		/// </summary>
		/// <returns></returns>
		public override void BuildUserInterfaces()
		{
			m_UserInterfacesSection.Enter();

			try
			{
				Clear();

				IEnumerable<TUserInterface> uis =
					m_Theme.Core
						   .Originators
						   .OfType<TOriginator>()
						   .Select(originator => CreateUserInterface(originator));

				m_UserInterfaces.AddRange(uis);

				ReassignUserInterfaces();
			}
			finally
			{
				m_UserInterfacesSection.Leave();
			}
		}

		/// <summary>
		/// Assigns the rooms to the existing user interfaces.
		/// </summary>
		public override void ReassignUserInterfaces()
		{
			AssignUserInterfaces(GetKrangAtHomeRooms());
		}

		/// <summary>
		/// Assigns the rooms to the existing user interfaces.
		/// </summary>
		public override void AssignUserInterfaces(IEnumerable<IRoom> rooms)
		{
			AssignUserInterfaces(rooms.OfType<IKrangAtHomeRoom>());
		}

		/// <summary>
		/// Assigns the rooms to the existing user interfaces.
		/// </summary>
		public void AssignUserInterfaces(IEnumerable<IKrangAtHomeRoom> rooms)
		{
			m_UserInterfacesSection.Enter();

			try
			{
				foreach (IKrangAtHomeRoom room in rooms)
				{
					foreach (TUserInterface ui in m_UserInterfaces)
					{
						if (RoomContainsOriginator(room, ui))
							ui.SetRoom(room);
					}
				}
			}
			finally
			{
				m_UserInterfacesSection.Leave();
			}
		}

		/// <summary>
		/// Gets the instantiated user interfaces.
		/// </summary>
		public override IEnumerable<IUserInterface> GetUserInterfaces()
		{
			return m_UserInterfacesSection.Execute(() => m_UserInterfaces.Cast<IUserInterface>().ToArray());
		}

		#endregion

		#region Protected Methods

		/// <summary>
		/// Instantiates the user interface for the given originator.
		/// </summary>
		/// <param name="originator"></param>
		/// <returns></returns>
		protected abstract TUserInterface CreateUserInterface(TOriginator originator);

		/// <summary>
		/// Returns true if the room contains the originator in the given ui.
		/// </summary>
		/// <param name="room"></param>
		/// <param name="ui"></param>
		/// <returns></returns>
		protected abstract bool RoomContainsOriginator(IKrangAtHomeRoom room, TUserInterface ui);

		/// <summary>
		/// Gets the rooms for the user interfaces.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IKrangAtHomeRoom> GetKrangAtHomeRooms()
		{
			return Theme.Core.Originators.GetChildren<IKrangAtHomeRoom>();
		}

		#endregion
	}
}