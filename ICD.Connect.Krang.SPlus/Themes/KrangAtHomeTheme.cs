using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusRemote;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusTouchpanel;
using ICD.Connect.Routing.RoutingCaches;
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

		private KrangAtHomeRouting m_KrangAtHomeRouting;

		private ICore m_Core;
		private KrangAtHomeSourceGroupManager m_KrangAtHomeSourceGroupManager;

		#region Properties

		public ICore Core { get { return m_Core = m_Core ?? ServiceProvider.GetService<ICore>(); } }

		public KrangAtHomeRouting KrangAtHomeRouintg { get { return m_KrangAtHomeRouting; }}

		public KrangAtHomeSourceGroupManager KrangAtHomeSourceGroupManager{ get { return m_KrangAtHomeSourceGroupManager; }}

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
			factory.LoadOriginators<IKrangAtHomeSource>();
			factory.LoadOriginators<IKrangAtHomeSourceGroup>();
			factory.LoadOriginators<IRoutingGraph>();
			factory.LoadOriginators<IKrangAtHomeRoom>();



			IRoutingGraph routingGraph = ServiceProvider.GetService<IRoutingGraph>();

			m_KrangAtHomeRouting = new KrangAtHomeRouting(routingGraph);

			m_KrangAtHomeSourceGroupManager = new KrangAtHomeSourceGroupManager(m_KrangAtHomeRouting);

			try
			{
				IEnumerable<IKrangAtHomeSourceGroup> sourceGroups = factory.GetOriginators<IKrangAtHomeSourceGroup>();

				if (sourceGroups != null)
					m_KrangAtHomeSourceGroupManager.AddSourceGroup(sourceGroups);
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

			m_KrangAtHomeRouting = null;
		}

		#endregion
	}
}