using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Settings;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusRemote;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusTouchpanel;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Protocol.Crosspoints;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
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

		private Xp3 m_CachedXp3;
		private ICore m_CachedCore;

		private readonly Dictionary<int, KrangAtHomeMultiRoomRouting> m_MulitRoomRoutings;

		#region Properties

		public ICore Core { get { return m_CachedCore = m_CachedCore ?? ServiceProvider.GetService<ICore>(); } }

		public KrangAtHomeRouting KrangAtHomeRouting { get; private set; }

		public KrangAtHomeSourceGroupManager KrangAtHomeSourceGroupManager { get; private set; }

		public int SystemId { get; set; }

		public Dictionary<int, KrangAtHomeMultiRoomRouting> MulitRoomRoutings { get { return m_MulitRoomRoutings; } }

		public Xp3 Xp3
		{
			get
			{
				if (m_CachedXp3 != null)
					return m_CachedXp3;

				if (ServiceProvider.TryGetService<Xp3>() == null)
					ServiceProvider.AddService(new Xp3());

				return m_CachedXp3 = ServiceProvider.GetService<Xp3>();
			}
		}

		public EquipmentCrosspointManager EquipmentCrosspointManager
		{
			get { return Xp3.GetOrCreateSystem(SystemId).GetOrCreateEquipmentCrosspointManager(); }
		} 

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangAtHomeTheme()
		{
			m_UiFactories = new IcdHashSet<IKrangAtHomeUserInterfaceFactory>
			{
				new KrangAtHomeTouchpanelUiFactory(this),
				new KrangAtHomeRemoteUiFactory(this),
				new KrangAtHomeMultiRoomRoutingUiFactory(this)
			};

			m_UiFactoriesSection = new SafeCriticalSection();

			m_MulitRoomRoutings = new Dictionary<int, KrangAtHomeMultiRoomRouting>();

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
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(KrangAtHomeThemeSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.SystemId = SystemId;

			settings.MultiRoomRoutings.Clear();
			foreach (var kvp in MulitRoomRoutings)
				settings.MultiRoomRoutings.Add(kvp.Key, kvp.Value.CopySettings());
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(KrangAtHomeThemeSettings settings, IDeviceFactory factory)
		{
			try
			{
// Ensure other originators are loaded
				factory.LoadOriginators<ISource>();
				factory.LoadOriginators<IKrangAtHomeSourceGroup>();
				factory.LoadOriginators<IRoutingGraph>();
				factory.LoadOriginators<IKrangAtHomeRoom>();
			}
			catch (Exception e)
			{
				Log(eSeverity.Critical, e, "Exception loading KrangAtHomeTheme - Originator Preload");
			}

			IRoutingGraph routingGraph = ServiceProvider.GetService<IRoutingGraph>();

			KrangAtHomeRouting = new KrangAtHomeRouting(routingGraph);

			KrangAtHomeSourceGroupManager = new KrangAtHomeSourceGroupManager(KrangAtHomeRouting);

			try
			{
				IEnumerable<ISource> sources = factory.GetOriginators<ISource>();
				KrangAtHomeRouting.AddSources(sources);
			}
			catch
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

			// Apply Multi-Room Routing Settings
			if (settings.SystemId.HasValue && settings.SystemId.Value != 0)
			{
				SystemId = settings.SystemId.Value;
				settings.MultiRoomRoutings.ForEach(kvp => ApplyMultiRoomRouting(kvp.Value, factory));
			}


			try
			{
				IEnumerable<IKrangAtHomeRoom> rooms = factory.GetOriginators<IKrangAtHomeRoom>();

				if (rooms != null)
					ApplyThemeToRooms(rooms);
			}
			catch
			{
			}


			base.ApplySettingsFinal(settings, factory);
		}

		private void ApplyMultiRoomRouting(KrangAtHomeMultiRoomRoutingSettings settings, IDeviceFactory factory)
		{
			KrangAtHomeMultiRoomRouting multiRoomRouting = new KrangAtHomeMultiRoomRouting();

			multiRoomRouting.ApplySettings(this, settings, factory);

			MulitRoomRoutings.Add(multiRoomRouting.EquipmentId, multiRoomRouting);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			foreach (KrangAtHomeMultiRoomRouting m in MulitRoomRoutings.Values)
				m.ClearSettings();

			MulitRoomRoutings.Clear();

			if (m_CachedXp3 != null && SystemId != 0)
			{
				CrosspointSystem system = m_CachedXp3.GetSystem(SystemId);
				if (system != null)
					system.Dispose();
			}

			SystemId = 0;

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
			if (m_CachedXp3 != null)
				yield return Xp3;
		}

		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}