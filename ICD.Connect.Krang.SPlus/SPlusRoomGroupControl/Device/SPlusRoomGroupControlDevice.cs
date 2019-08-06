using System;
using System.Collections.Generic;
using ICD.Common.Utils.Services;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Krang.SPlus.RoomGroups;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Cores;
using ICD.Connect.Settings.Originators.Simpl;

namespace ICD.Connect.Krang.SPlus.SPlusRoomGroupControl.Device
{
	public sealed class SPlusRoomGroupControlDevice : AbstractDevice<SPlusRoomGroupControlDeviceSettings>, ISimplOriginator, ISPlusRoomGroupControl
	{
		private int m_RoomGroupId;
		
		private SPlusRoomGroup m_CachedRoomGroup;
		 
		// Load room group at runtime to prevent cyclic dependency
		private SPlusRoomGroup RoomGroup
		{
			get
			{
				if (m_CachedRoomGroup == null)
					m_CachedRoomGroup = CachedCore.Originators.GetChild<SPlusRoomGroup>(m_RoomGroupId);
				return m_CachedRoomGroup;
			}
		}

		private ICore m_CachedCore;

		private ICore CachedCore
		{
			get
			{
				if (m_CachedCore == null)
					m_CachedCore = ServiceProvider.TryGetService<ICore>();
				return m_CachedCore;
			}
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return true;
		}

		public event EventHandler<RequestShimResyncEventArgs> OnRequestShimResync;

		#region Methods

		public void AllOff()
		{
			SetSource(null, eSourceTypeRouted.AudioVideo);
		}

		public void SetSource(IKrangAtHomeSourceBase source, eSourceTypeRouted routed)
		{
			if (RoomGroup == null)
				return;

			foreach (IRoom room in RoomGroup.GetRooms())
			{
				IKrangAtHomeRoom krangRoom = room as IKrangAtHomeRoom;
				if (krangRoom == null)
					continue;
				
				krangRoom.SetSource(source, routed);
			}
		}

		public void SetSource(int sourceId, eSourceTypeRouted routed)
		{
			IKrangAtHomeSourceBase source = CachedCore.Originators.GetChild<IKrangAtHomeSourceBase>(sourceId);
            SetSource(source, routed);       
		}

		#endregion

		#region Settings
		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(SPlusRoomGroupControlDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_RoomGroupId = settings.RoomGroupId;
			//RoomGroup = factory.GetOriginatorById<SPlusRoomGroup>(settings.RoomGroupId);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_RoomGroupId = 0;
			//RoomGroup = null;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SPlusRoomGroupControlDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.RoomGroupId = m_RoomGroupId;
			//settings.RoomGroupId = RoomGroup == null ? 0 : RoomGroup.Id;
		}
		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			if (RoomGroup != null)
				yield return RoomGroup;
		}

		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("AllOff", "Turns off all rooms in the group", () => AllOff());
			yield return
				new GenericConsoleCommand<int,eSourceTypeRouted>("SetSource", "Sets the given source id to all rooms", (s,t) => SetSource(s,t));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}