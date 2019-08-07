using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.EventArgs;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Settings.Originators;
using ICD.Connect.Themes.UserInterfaces;

namespace ICD.Connect.Krang.SPlus.Themes
{
	public abstract class AbstractKrangAtHomeUi<T> : IKrangAtHomeUserInterface
		where T : class, IKrangAtHomeUiDevice
	{
		#region Fields

		private readonly KrangAtHomeTheme m_Theme;

		private readonly T m_UiDevice;

		#endregion

		#region Properties

		public T UiDevice { get { return m_UiDevice; } }

		protected KrangAtHomeTheme Theme {get { return m_Theme; }}

		protected IKrangAtHomeRoom Room { get; set; }

		/// <summary>
		/// Gets the target instance attached to this UI (i.e. the Panel, KeyPad, etc).
		/// </summary>
		public object Target { get { return m_UiDevice; } }

		/// <summary>
		/// Tells the UI that it should be considered ready to use.
		/// For example updating the online join on a panel or starting a long-running process that should be delayed.
		/// </summary>
		public virtual void Activate()
		{
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractKrangAtHomeUi(KrangAtHomeTheme theme, T uiDevice)
		{
			m_UiDevice = uiDevice;
			m_Theme = theme;

		}

		#region Methods


		/// <summary>
		/// Release resources.
		/// </summary>
		public virtual void Dispose()
		{
			Unsubscribe(m_UiDevice);

			SetRoom(null);
		}

		#endregion

		#region Room Select

		/// <summary>
		/// Sets the current room for routing operations.
		/// </summary>
		/// <param name="roomId"></param>
		private void SetRoomId(int roomId)
		{
			//ServiceProvider.GetService<ILoggerService>()
			//               .AddEntry(eSeverity.Informational, "{0} setting room to {1}", this, roomId);

			IKrangAtHomeRoom room = GetRoom(roomId);
			SetRoom(room);
		}

		#endregion

		#region Source Select

		private void SetSourceId(int sourceId, eSourceTypeRouted type)
		{
			if (sourceId == 0)
			{
				SetSource(null, type);
				return;
			}

			IKrangAtHomeSourceBase source = GetSourceId(sourceId);

			if (source == null)
				return;

			SetSource(source, type);
		}

		/// <summary>
		/// Sets the selected source for the room
		/// </summary>
		/// <param name="sourceBase"></param>
		/// <param name="type"></param>
		protected void SetSource(IKrangAtHomeSourceBase sourceBase, eSourceTypeRouted type)
		{
			if (Room == null)
				return;

			if (sourceBase == null)
			{
				Room.SetSource(null, type);
				return;
			}

			Room.SetSource(sourceBase, type);
		}

		protected IKrangAtHomeSourceBase GetSourceId(int id)
		{
			if (Room == null)
				return null;

			return Room.GetSourceId(id);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sets the current room for routing operations.
		/// </summary>
		/// <param name="genericRoom"></param>
		public void SetRoom(IRoom genericRoom)
		{
			IKrangAtHomeRoom room = genericRoom as IKrangAtHomeRoom;
			if (room == null && genericRoom != null)
				ServiceProvider.GetService<ILoggerService>()
				               .AddEntry(eSeverity.Error, "{0} Room is not KrangAtHomeRoom: {1}", this, genericRoom);

			if (room == Room)
				return;

			Unsubscribe(Room);

			Room = room;
			Subscribe(Room);

			// Update Volume Control
			if (Room != null)
				SetVolumeControl(Room.ActiveVolumeControl);
			else
				SetVolumeControl(null);

			RaiseRoomInfo();
		}

		private void SetVolumeControl()
		{
			SetVolumeControl(Room == null ? null : Room.ActiveVolumeControl);
		}

		private void SetVolumeControl(IVolumeDeviceControl activeVolumeControl)
		{
			if (activeVolumeControl != null)
				UiDevice.SetVolumeControl(activeVolumeControl.DeviceControlInfo);
			else
			{
				DeviceControlInfo info = new DeviceControlInfo(0,0);
				UiDevice.SetVolumeControl(info);
			}
		}

		/// <summary>
		/// Gets the room for the given room id.
		/// </summary>
		/// <returns></returns>
		[CanBeNull]
		private IKrangAtHomeRoom GetRoom(int id)
		{
			IOriginator output;

			m_Theme.Core.Originators.TryGetChild(id, out output);
			return output as IKrangAtHomeRoom;
		}

		protected abstract void RaiseRoomInfo();

		#endregion

		#region Room Callbacks

		/// <summary>
		/// Subscribe to the room events.
		/// </summary>
		/// <param name="room"></param>
		private void Subscribe(IKrangAtHomeRoom room)
		{
			if (room == null)
				return;

			room.OnActiveSourcesChange += RoomOnActiveSourcesChange;
			room.OnActiveVolumeControlChanged += RoomOnActiveVolumeControlChanged;
		}

		/// <summary>
		/// Unsubscribe from the room events.
		/// </summary>
		/// <param name="room"></param>
		private void Unsubscribe(IKrangAtHomeRoom room)
		{
			if (room == null)
				return;

			room.OnActiveSourcesChange -= RoomOnActiveSourcesChange;
			room.OnActiveVolumeControlChanged -= RoomOnActiveVolumeControlChanged;
		}

		/// <summary>
		/// Raised when source/s become actively/inactively routed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		protected abstract void RoomOnActiveSourcesChange(object sender, EventArgs eventArgs);

		private void RoomOnActiveVolumeControlChanged(object sender, GenericEventArgs<IVolumeDeviceControl> args)
		{
			SetVolumeControl(args.Data);
		}

		#endregion

		#region Panel Callback

		protected virtual void Subscribe(T panel)
		{
			if (panel == null)
				return;
			
			panel.OnRequestRefresh += PanelOnRequestRefresh;
			panel.OnSetRoomId += PanelOnSetRoomId;
			panel.OnSetAudioSourceId += PanelOnSetAudioSourceId;
			panel.OnSetVideoSourceId += PanelOnSetVideoSourceId;
		}

		protected virtual void Unsubscribe(T panel)
		{
			if (panel == null)
				return;

			panel.OnRequestRefresh -= PanelOnRequestRefresh;
			panel.OnSetRoomId -= PanelOnSetRoomId;
			panel.OnSetAudioSourceId -= PanelOnSetAudioSourceId;
			panel.OnSetVideoSourceId -= PanelOnSetVideoSourceId;
		}


		private void PanelOnSetRoomId(object sender, SetRoomIdApiEventArgs args)
		{
			SetRoomId(args.Data);
		}

		private void PanelOnSetAudioSourceId(object sender, SetAudioSourceIdApiEventArgs args)
		{
			SetSourceId(args.Data, eSourceTypeRouted.Audio);
		}

		private void PanelOnSetVideoSourceId(object sender, SetVideoSourceIdApiEventArgs args)
		{
			SetSourceId(args.Data, eSourceTypeRouted.AudioVideo);
		}

		/// <summary>
		/// Panel requesting a refresh
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected virtual void PanelOnRequestRefresh(object sender, EventArgs args)
		{
			SetVolumeControl();
			RaiseRoomInfo();
		}

		

		#endregion

		/// <summary>
		/// Gets the room attached to this UI.
		/// </summary>
		IRoom IUserInterface.Room { get { return Room; } }
	}
}
