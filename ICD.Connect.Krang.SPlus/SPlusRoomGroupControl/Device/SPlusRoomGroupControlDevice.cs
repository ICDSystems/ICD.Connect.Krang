using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Devices;
using ICD.Connect.Krang.SPlus.RoomGroups;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Originators.Simpl;

namespace ICD.Connect.Krang.SPlus.SPlusRoomGroupControl.Device
{
	public sealed class SPlusRoomGroupControlDevice : AbstractDevice<SPlusRoomGroupControlDeviceSettings>, ISimplOriginator, ISPlusRoomGroupControl
	{

		private SPlusRoomGroup m_RoomGroup;

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
			if (m_RoomGroup == null)
				return;

			foreach (IRoom room in m_RoomGroup.GetRooms())
			{
				IKrangAtHomeRoom krangRoom = room as IKrangAtHomeRoom;
				if (krangRoom == null)
					continue;
				
				krangRoom.SetSource(source, routed);
			}
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

			m_RoomGroup = factory.GetOriginatorById<SPlusRoomGroup>(settings.RoomGroupId);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_RoomGroup = null;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SPlusRoomGroupControlDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.RoomGroupId = m_RoomGroup == null ? 0 : m_RoomGroup.Id;
		}
		#endregion
	}
}