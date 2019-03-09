using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Krang.SPlus.RoomGroups;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusRemote;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusTouchpanel;
using ICD.Connect.Routing.Endpoints;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Protocol.Crosspoints;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
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

		private readonly IcdHashSet<IKrangAtHomeSource> m_AudioSources;
		private readonly IcdHashSet<IKrangAtHomeSource> m_VideoSources;

		// Room Groups - key = index, value = group originator id
		private readonly Dictionary<int, SPlusRoomGroup> m_AudioRoomGroups;
		private readonly Dictionary<int, SPlusRoomGroup> m_VideoRoomGroups;

		private Xp3 m_CachedXp3;
		private ICore m_CachedCore;

		#region Properties

		public ICore Core { get { return m_CachedCore = m_CachedCore ?? ServiceProvider.GetService<ICore>(); } }

		public KrangAtHomeRouting KrangAtHomeRouting { get; private set; }

		public KrangAtHomeSourceGroupManager KrangAtHomeSourceGroupManager { get; private set; }

		public int SystemId { get; set; }

		public NonCachingEquipmentCrosspoint AudioEquipment { get; set; }

		public NonCachingEquipmentCrosspoint VideoEquipment { get; set; }

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

			m_AudioSources = new IcdHashSet<IKrangAtHomeSource>();
			m_VideoSources = new IcdHashSet<IKrangAtHomeSource>();

			m_AudioRoomGroups = new Dictionary<int, SPlusRoomGroup>();
			m_VideoRoomGroups = new Dictionary<int, SPlusRoomGroup>();

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

		public IEnumerable<IKrangAtHomeSource> GetAudioSources()
		{
			return m_AudioSources.OrderBy(s => (s as ISourceDestinationBase).Order);
		}

		public IEnumerable<IKrangAtHomeSource> GetVideoSources()
		{
			return m_VideoSources.OrderBy(s => (s as ISourceDestinationBase).Order);
		}

		public IEnumerable<KeyValuePair<int, SPlusRoomGroup>> GetAudioRoomGroups()
		{
			return m_AudioRoomGroups;
		}

		public IEnumerable<KeyValuePair<int, SPlusRoomGroup>> GetVideoRoomGroups()
		{
			return m_VideoRoomGroups;
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
			settings.AudioEquipmentId = AudioEquipment == null ? (int?)null : AudioEquipment.Id;
			settings.VideoEquipmentId = VideoEquipment == null ? (int?)null : VideoEquipment.Id;

			settings.AudioSourceIds.Clear();
			settings.AudioSourceIds.AddRange(m_AudioSources.Select(s => s.Id));

			settings.VideoSourceIds.Clear();
			settings.VideoSourceIds.AddRange(m_VideoSources.Select(s => s.Id));


			settings.AudioRoomGroupIds.Clear();
			settings.AudioRoomGroupIds.AddRange(m_AudioRoomGroups.Select(kvp => new KeyValuePair<int, int>(kvp.Key, kvp.Value.Id)));

			settings.VideoRoomGroupIds.Clear();
			settings.VideoRoomGroupIds.AddRange(m_VideoRoomGroups.Select(kvp => new KeyValuePair<int, int>(kvp.Key, kvp.Value.Id)));
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

			try{
				m_AudioSources.Clear();
				m_AudioSources.AddRange(settings.AudioSourceIds.Select(id => factory.GetOriginatorById<IKrangAtHomeSource>(id)));

				m_AudioRoomGroups.Clear();
				m_AudioRoomGroups.AddRange(settings.AudioRoomGroupIds.Select(kvp => new KeyValuePair<int, SPlusRoomGroup>(kvp.Key,factory.GetOriginatorById<SPlusRoomGroup>(kvp.Value))));

			}
			catch (Exception e)
			{
				Log(eSeverity.Critical, e, "Exception loading KrangAtHomeTheme - Multi-Room Audio");
			}

			try
			{
				m_VideoSources.Clear();
				foreach (int s in settings.VideoSourceIds)
				{
					try
					{
						var source = factory.GetOriginatorById<IKrangAtHomeSource>(s);
						if (source != null)
							m_VideoSources.Add(source);
						else
						{
							Log(eSeverity.Error, "Source at {0} was null for some reason?", s);
						}
					}
					catch
					{
						Log(eSeverity.Critical, "Error adding source {0}", s);
					}
				}
				m_VideoSources.AddRange(settings.VideoSourceIds.Select(id => factory.GetOriginatorById<IKrangAtHomeSource>(id)));

				m_VideoRoomGroups.Clear();
					m_VideoRoomGroups.AddRange(settings.VideoRoomGroupIds.Select(kvp => new KeyValuePair<int, SPlusRoomGroup>(kvp.Key, factory.GetOriginatorById<SPlusRoomGroup>(kvp.Value))));

			}
			catch (Exception e)
			{
				Log(eSeverity.Critical, e, "Exception loading KrangAtHomeTheme - Multi-Room Video");
				throw e;
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



			try
			{
// Setup the equipment crosspoints
				SystemId = settings.SystemId ?? 0;
			
				if (settings.AudioEquipmentId.HasValue)
				{
					AudioEquipment = new NonCachingEquipmentCrosspoint(settings.AudioEquipmentId.Value, "AudioEquipment");
					EquipmentCrosspointManager.RegisterCrosspoint(AudioEquipment);
				}

				if (settings.VideoEquipmentId.HasValue)
				{
					VideoEquipment = new NonCachingEquipmentCrosspoint(settings.VideoEquipmentId.Value, "VideoEquipment");
					EquipmentCrosspointManager.RegisterCrosspoint(VideoEquipment);
				}
			}
			catch (Exception e)
			{
				Log(eSeverity.Critical, e, "Exception loading KrangAtHomeTheme - Multi-Room Crosspoints");
			}

				try
				{
					IEnumerable<IKrangAtHomeRoom> rooms = factory.GetOriginators<IKrangAtHomeRoom>();

					if (rooms != null)
						ApplyThemeToRooms(rooms);
				}
				catch (Exception)
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

			if (AudioEquipment != null)
			{
				EquipmentCrosspointManager.UnregisterCrosspoint(AudioEquipment);
				AudioEquipment.Dispose();
				AudioEquipment = null;
			}

			if (VideoEquipment != null)
			{
				EquipmentCrosspointManager.UnregisterCrosspoint(VideoEquipment);
				VideoEquipment.Dispose();
				VideoEquipment = null;
			}

			SystemId = 0;

			KrangAtHomeRouting = null;

			m_AudioSources.Clear();
			m_VideoSources.Clear();
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