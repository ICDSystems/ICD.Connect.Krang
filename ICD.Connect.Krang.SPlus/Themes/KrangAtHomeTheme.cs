using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Connect.API.Nodes;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusRemote;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusTouchpanel;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Cores;
using ICD.Connect.Themes;

namespace ICD.Connect.Krang.SPlus.Themes
{
	public sealed class KrangAtHomeTheme : AbstractTheme<KrangAtHomeThemeSettings>
	{

		private readonly IcdHashSet<IKrangAtHomeUserInterfaceFactory> m_UiFactories;
		private readonly SafeCriticalSection m_UiFactoriesSection;

		private ICore m_CachedCore;

		#region Properties

		public ICore Core { get { return m_CachedCore = m_CachedCore ?? ServiceProvider.GetService<ICore>(); } }

		public KrangAtHomeRouting KrangAtHomeRouting { get; private set; }

		public KrangAtHomeSourceGroupManager KrangAtHomeSourceGroupManager { get; private set; }

		#endregion

		public KrangAtHomeTheme()
		{
			m_UiFactories = new IcdHashSet<IKrangAtHomeUserInterfaceFactory>
			{
				new KrangAtHomeTouchpanelUiFactory(this),
				new KrangAtHomeRemoteUiFactory(this)
			};

			m_UiFactoriesSection = new SafeCriticalSection();

			Core.Originators.OnChildrenChanged += OriginatorsOnChildrenChanged;
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			Core.Originators.OnChildrenChanged -= OriginatorsOnChildrenChanged;

			base.DisposeFinal(disposing);
		}

		private void OriginatorsOnChildrenChanged(object sender, EventArgs e)
		{
			ReassignRooms();
		}

		/// <summary>
		/// Clears the instantiated user interfaces.
		/// </summary>
		public override void ClearUserInterfaces()
		{
			m_UiFactoriesSection.Enter();

			try
			{
				m_UiFactories.ForEach(f => f.Clear());
			}
			finally
			{
				m_UiFactoriesSection.Leave();
			}
		}

		/// <summary>
		/// Clears and rebuilds the user interfaces.
		/// </summary>
		public override void BuildUserInterfaces()
		{
			m_UiFactoriesSection.Enter();

			try
			{
				m_UiFactories.ForEach(f => f.BuildUserInterfaces());
			}
			finally
			{
				m_UiFactoriesSection.Leave();
			}
		}

		private void ApplyThemeToRooms(IEnumerable<IKrangAtHomeRoom> rooms)
		{
			rooms.ForEach(room => room.ApplyTheme(this));
		}

		#region User Interfaces

		/// <summary>
		/// Reassigns rooms to the existing user interfaces.
		/// </summary>
		private void ReassignRooms()
		{
			m_UiFactoriesSection.Enter();

			try
			{
				m_UiFactories.ForEach(f => f.ReassignUserInterfaces());
			}
			finally
			{
				m_UiFactoriesSection.Leave();
			}
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(KrangAtHomeThemeSettings settings, IDeviceFactory factory)
		{
			// Ensure other originators are loaded
			factory.LoadOriginators<IKrangAtHomeSourceGroup>();
			factory.LoadOriginators<IRoutingGraph>();
			factory.LoadOriginators<IKrangAtHomeRoom>();



			IRoutingGraph routingGraph = ServiceProvider.GetService<IRoutingGraph>();

			KrangAtHomeRouting = new KrangAtHomeRouting(routingGraph);

			KrangAtHomeSourceGroupManager = new KrangAtHomeSourceGroupManager(KrangAtHomeRouting);


			try
			{
				IEnumerable<IKrangAtHomeRoom> rooms = factory.GetOriginators<IKrangAtHomeRoom>();

				if (rooms != null)
					ApplyThemeToRooms(rooms);
			}
			catch (Exception)
			{
			}

			try
			{
				IEnumerable<IKrangAtHomeSourceGroup> sourceGroups = factory.GetOriginators<IKrangAtHomeSourceGroup>();

				if (sourceGroups != null)
					KrangAtHomeSourceGroupManager.AddSourceGroup(sourceGroups);
			}
			catch
			{
				
			}

			base.ApplySettingsFinal(settings, factory);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			KrangAtHomeRouting = null;
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase consoleNode in GetBaseConsoleNodes())
				yield return consoleNode;

			if (KrangAtHomeRouting != null)
				yield return KrangAtHomeRouting;
			if (KrangAtHomeSourceGroupManager != null)
				yield return KrangAtHomeSourceGroupManager;
		}

		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}