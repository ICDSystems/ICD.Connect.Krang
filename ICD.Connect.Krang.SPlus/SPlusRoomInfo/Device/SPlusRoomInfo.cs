using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Originators.Simpl;

namespace ICD.Connect.Krang.SPlus.SPlusRoomInfo.Device
{
	public sealed class SPlusRoomInfo : AbstractDevice<SPlusRoomInfoSettings>, ISPlusRoomInfo
	{

		#region Fields

		private IKrangAtHomeRoom m_Room;

		#endregion

		#region Properties

		public string RoomName
		{
			get { return m_Room == null ? null : m_Room.Name; }
		}

		#endregion

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			throw new NotImplementedException();
		}

		public event EventHandler<RequestShimResyncEventArgs> OnRequestShimResync;


		#region Room Callbacks

		private void Subscribe(IKrangAtHomeRoom room)
		{
			if (room == null)
				return;

			room.OnActiveSourcesChange += RoomOnActiveSourcesChange;
			room.OnActiveVolumeControlChanged += RoomOnActiveVolumeControlChanged;
			room.OnSettingsApplied += RoomOnSettingsApplied;
		}

		private void Unsubscribe(IKrangAtHomeRoom room)
		{
			if (room == null)
				return;

			room.OnActiveSourcesChange -= RoomOnActiveSourcesChange;
			room.OnActiveVolumeControlChanged -= RoomOnActiveVolumeControlChanged;
			room.OnSettingsApplied -= RoomOnSettingsApplied;
		}

		private void RoomOnActiveSourcesChange(object sender, EventArgs eventArgs)
		{
			throw new NotImplementedException();
		}

		private void RoomOnActiveVolumeControlChanged(object sender, GenericEventArgs<IVolumeDeviceControl> genericEventArgs)
		{
			throw new NotImplementedException();
		}

		private void RoomOnSettingsApplied(object sender, EventArgs eventArgs)
		{
			throw new NotImplementedException();
		}

		#endregion


		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(SPlusRoomInfoSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_Room = factory.GetOriginatorById<IKrangAtHomeRoom>(settings.RoomId);
			Subscribe(m_Room);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Unsubscribe(m_Room);
			m_Room = null;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SPlusRoomInfoSettings settings)
		{
			base.CopySettingsFinal(settings);

			if (m_Room != null)
				settings.RoomId = m_Room.Id;
		}

		#endregion
	}
}