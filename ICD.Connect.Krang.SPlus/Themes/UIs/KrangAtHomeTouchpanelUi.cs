using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Krang.SPlus.Devices;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.SPlus.Themes.UIs
{
	public sealed class KrangAtHomeTouchpanelUi : IKrangAtHomeUserInterface
	{
		private const ushort INDEX_NOT_FOUND = 0;
		private const ushort INDEX_START = 1;

		#region Fields

		private readonly KrangAtHomeTheme m_Theme;

		private IKrangAtHomeRoom m_Room;

		private IVolumeDeviceControl m_VolumeControl;

		private IVolumePositionDeviceControl m_VolumePositionControl;

		private IVolumeMuteFeedbackDeviceControl m_VolumeMuteFeedbackControl;

		private readonly KrangAtHomeSPlusTouchpanelDevice m_Panel;

		/// <summary>
		/// List of rooms and indexes
		/// </summary>
		private BiDictionary<ushort, IKrangAtHomeRoom> m_RoomListBiDictionary;

		private BiDictionary<ushort, ISimplSource> m_SourceListAudioBiDictionary;

		private BiDictionary<ushort, ISimplSource> m_SourceListVideoBiDictionary;

		#endregion

		#region Properties

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangAtHomeTouchpanelUi(KrangAtHomeTheme theme,KrangAtHomeSPlusTouchpanelDevice panel)
		{
			m_RoomListBiDictionary = new BiDictionary<ushort, IKrangAtHomeRoom>();
			m_SourceListAudioBiDictionary = new BiDictionary<ushort, ISimplSource>();
			m_SourceListVideoBiDictionary = new BiDictionary<ushort, ISimplSource>();

			m_Panel = panel;
			m_Theme = theme;

			Subscribe(m_Panel);
		}

		#region Methods


		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Unsubscribe(m_Panel);

			SetRoom(null);
			
			m_RoomListBiDictionary.Clear();
			m_SourceListAudioBiDictionary.Clear();
			m_SourceListVideoBiDictionary.Clear();
		}
		
		#endregion

		#region Room Select

		/// <summary>
		/// Sets the current room for routing operations.
		/// </summary>
		/// <param name="roomId"></param>
		public void SetRoomId(int roomId)
		{
			//ServiceProvider.GetService<ILoggerService>()
			//               .AddEntry(eSeverity.Informational, "{0} setting room to {1}", this, roomId);

			IKrangAtHomeRoom room = GetRoom(roomId);
			SetRoom(room);
		}

		/// <summary>
		/// Sets the current room, based on the room list index
		/// </summary>
		/// <param name="roomIndex">index of the room to set</param>
		public void SetRoomIndex(ushort roomIndex)
		{
			ServiceProvider.GetService<ILoggerService>()
			               .AddEntry(eSeverity.Informational, "{0} setting room index to {1}", this, roomIndex);

			IKrangAtHomeRoom room;
			if (!m_RoomListBiDictionary.TryGetValue(roomIndex, out room))
				return;

			SetRoom(room);
		}

		#endregion

		#region Source Select

		private void SetVideoSourceIndex(ushort sourceIndex)
		{
			// sourceIndex of 0 is used for "Off" - pass it along w/o lookup
			if (sourceIndex == 0)
			{
				SetSource(null, eSourceTypeRouted.Video);
				return;
			}

			ISimplSource source;

			if (!m_SourceListVideoBiDictionary.TryGetValue(sourceIndex, out source))
				return;

			SetSource(source, eSourceTypeRouted.Video);
		}

		private void SetAudioSourceIndex(ushort sourceIndex)
		{
			// sourceIndex of 0 is used for "Off" - pass it along w/o lookup
			if (sourceIndex == 0)
			{
				SetSource(null, eSourceTypeRouted.Audio);
				return;
			}

			ISimplSource source;

			if (!m_SourceListAudioBiDictionary.TryGetValue(sourceIndex, out source))
				return;

			SetSource(source, eSourceTypeRouted.Audio);
		}

		private void SetSourceId(int sourceId, eSourceTypeRouted type)
		{
			if (sourceId == 0)
			{
				SetSource(null, type);
				return;
			}

			ISimplSource source = GetSourceId(sourceId);

			if (source == null)
				return;

			SetSource(source, type);
		}

		/// <summary>
		/// Sets the selected source for the room
		/// </summary>
		/// <param name="source"></param>
		/// <param name="type"></param>
		private void SetSource(ISimplSource source, eSourceTypeRouted type)
		{
			if (m_Room == null)
				return;

			m_Room.SetSource(source, type);
		}

		private ISimplSource GetSourceId(int id)
		{
			if (m_Room == null)
				return null;

			return m_Room.GetSourceId(id);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sets the current room for routing operations.
		/// </summary>
		/// <param name="room"></param>
		public void SetRoom(IKrangAtHomeRoom room)
		{
			//ServiceProvider.GetService<ILoggerService>().AddEntry(eSeverity.Informational, "{0} setting room to {1}", this, room);

			if (room == m_Room)
				return;

			Unsubscribe(m_Room);

			m_Room = room;
			Subscribe(m_Room);

			// Update Volume Control
			if (m_Room != null)
			{
				SetVolumeControl(m_Room.VolumeControl);
			}

			RaiseRoomInfo();
		}

		private void SetVolumeControl(IVolumeDeviceControl control)
		{
			if (m_VolumeControl == control)
				return;

			Unsubscribe(m_VolumeControl);

			m_VolumeControl = control;

			Subscribe(m_VolumeControl);

			InstantiateVolumeControl(m_VolumeControl);
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

		private void RaiseRoomInfo()
		{
			if (m_Room == null)
			{
				m_Panel.SetRoomInfo(null, INDEX_NOT_FOUND);
				return;
			}

			ushort index;
			if (!m_RoomListBiDictionary.TryGetKey(m_Room, out index))
				index = INDEX_NOT_FOUND;

			m_Panel.SetRoomInfo(m_Room, index);

			RaiseSourceList();
		}

		private void RaiseRoomList()
		{
			BiDictionary<ushort, IKrangAtHomeRoom> roomListDictionary = new BiDictionary<ushort, IKrangAtHomeRoom>();

			ushort counter = INDEX_START;
			foreach (IKrangAtHomeRoom room in m_Theme.Core.Originators.GetChildren<IKrangAtHomeRoom>())
			{
				roomListDictionary.Add(counter, room);
				counter++;
			}

			m_Panel.SetRoomList(roomListDictionary);

			m_RoomListBiDictionary = roomListDictionary;

			RaiseRoomInfo();
		}

		private void RaiseSourceList()
		{
			BiDictionary<ushort, ISimplSource> sourceListAudioBiDictionary = new BiDictionary<ushort, ISimplSource>();
			BiDictionary<ushort, ISimplSource> sourceListVideoBiDictionary = new BiDictionary<ushort, ISimplSource>();

			//ushort[] indexArray = {INDEX_START, INDEX_START};
			ushort audioListIndexCounter = INDEX_START;
			ushort videoListIndexCounter = INDEX_START;

			IEnumerable<ISimplSource> sources =
				m_Room == null
					? Enumerable.Empty<ISimplSource>()
					: m_Room.Originators.GetInstancesRecursive<ISimplSource>();

			foreach (ISimplSource source in sources)
			{

				if (source.SourceVisibility.HasFlag(SimplSource.eSourceVisibility.Audio))
				{
					sourceListAudioBiDictionary.Add(audioListIndexCounter, source);
					audioListIndexCounter++;
				}

				if (source.SourceVisibility.HasFlag(SimplSource.eSourceVisibility.Video))
				{
					sourceListVideoBiDictionary.Add(videoListIndexCounter, source);
					videoListIndexCounter++;
				}

			}
			m_Panel.SetAudioSourceList(sourceListAudioBiDictionary);
			m_Panel.SetVideoSourceList(sourceListVideoBiDictionary);

			m_SourceListAudioBiDictionary = sourceListAudioBiDictionary;
			m_SourceListVideoBiDictionary = sourceListVideoBiDictionary;
		}

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
			room.OnVolumeControlChanged += RoomOnVolumeControlChanged;
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
			room.OnVolumeControlChanged -= RoomOnVolumeControlChanged;
		}

		/// <summary>
		/// Raised when source/s become actively/inactively routed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void RoomOnActiveSourcesChange(object sender, EventArgs eventArgs)
		{
			ISimplSource source = m_Room == null ? null : m_Room.GetSource();

			m_Panel.SetSourceInfo(source, 0, eSourceTypeRouted.Video);
		}

		private void RoomOnVolumeControlChanged(object sender, GenericEventArgs<IVolumeDeviceControl> args)
		{
			SetVolumeControl(args.Data);
		}

		#endregion

		#region VolumeDeviceControl

		private void Subscribe(IVolumeDeviceControl volumeDevice)
		{
			if (volumeDevice == null)
				return;

			m_VolumePositionControl = volumeDevice as IVolumePositionDeviceControl;
			SubscribeVolumePositionDeviceControl(m_VolumePositionControl);

			m_VolumeMuteFeedbackControl = volumeDevice as IVolumeMuteFeedbackDeviceControl;
			SubscribeVolumeMuteFeedbackDeviceControl(m_VolumeMuteFeedbackControl);

		}

		private void Unsubscribe(IVolumeDeviceControl volumeDevice)
		{
			if (volumeDevice == null)
				return;

			UnsubscribeVolumePositionDeviceControl(m_VolumePositionControl);
			UnsubscribeVolumeMuteFeedbackDeviceControl(m_VolumeMuteFeedbackControl);
		}

		private void InstantiateVolumeControl(IVolumeDeviceControl volumeDevice)
		{
			// Test for VolumeControl being Null
			if (volumeDevice == null)
			{
				m_Panel.SetVolumeAvaliableControls(eVolumeLevelControlsAvaliable.None, eVolumeMuteControlsAvaliable.None);
				m_Panel.SetVolumeLevelFeedback(0);
				m_Panel.SetVolumeMuteFeedback(false);
				return;
			}

			//Cast to volume devices
			IVolumeRampDeviceControl volumeRampDevice = volumeDevice as IVolumeRampDeviceControl;
			IVolumePositionDeviceControl volumePositionDevice = volumeDevice as IVolumePositionDeviceControl;

			//Cast to mute devices
			IVolumeMuteBasicDeviceControl muteBasicDevice = volumeDevice as IVolumeMuteBasicDeviceControl;
			IVolumeMuteDeviceControl muteDiscreteDevice = volumeDevice as IVolumeMuteDeviceControl;
			IVolumeMuteFeedbackDeviceControl muteFeedbackDevice = volumeDevice as IVolumeMuteFeedbackDeviceControl;

			//Figure out volume control and feedback
			eVolumeLevelControlsAvaliable volumeControlsAvaliable = eVolumeLevelControlsAvaliable.None;
			float volumeLevelFeedback = 0;
			if (volumePositionDevice != null)
			{
				volumeControlsAvaliable = eVolumeLevelControlsAvaliable.Position;
				volumeLevelFeedback = volumePositionDevice.VolumePosition;
			}
			else if (volumeRampDevice != null)
				volumeControlsAvaliable = eVolumeLevelControlsAvaliable.Ramp;

			//Figure out mute control and feedback
			eVolumeMuteControlsAvaliable muteControlsAvaliable = eVolumeMuteControlsAvaliable.None;
			bool muteStateFeedback = false;
			if (muteFeedbackDevice != null)
			{
				muteControlsAvaliable = eVolumeMuteControlsAvaliable.Feedback;
				muteStateFeedback = muteFeedbackDevice.VolumeIsMuted;
			}
			else if (muteDiscreteDevice != null)
				muteControlsAvaliable = eVolumeMuteControlsAvaliable.Discrete;
			else if (muteBasicDevice != null)
				muteControlsAvaliable = eVolumeMuteControlsAvaliable.Toggle;

			// Send Feedback to Panel
			m_Panel.SetVolumeAvaliableControls(volumeControlsAvaliable, muteControlsAvaliable);
			m_Panel.SetVolumeLevelFeedback(volumeLevelFeedback);
			m_Panel.SetVolumeMuteFeedback(muteStateFeedback);
		}

		#region VolumePositionDeviceControl

		private void SubscribeVolumePositionDeviceControl(IVolumePositionDeviceControl control)
		{
			if (control == null)
				return;

			control.OnVolumeChanged += ControlOnVolumeChanged;
		}

		private void UnsubscribeVolumePositionDeviceControl(IVolumePositionDeviceControl control)
		{
			if (control == null)
				return;

			control.OnVolumeChanged -= ControlOnVolumeChanged;
		}

		private void ControlOnVolumeChanged(object sender, VolumeDeviceVolumeChangedEventArgs args)
		{
			m_Panel.SetVolumeLevelFeedback(args.VolumePosition);
		}

		#endregion

		#region MuteFeedbackDeviceControl

		private void SubscribeVolumeMuteFeedbackDeviceControl(IVolumeMuteFeedbackDeviceControl control)
		{
			if (control == null)
				return;

			control.OnMuteStateChanged += ControlOnMuteStateChanged;
		}

		private void UnsubscribeVolumeMuteFeedbackDeviceControl(IVolumeMuteFeedbackDeviceControl control)
		{
			if (control == null)
				return;

			control.OnMuteStateChanged -= ControlOnMuteStateChanged;
		}

		private void ControlOnMuteStateChanged(object sender, BoolEventArgs args)
		{
			m_Panel.SetVolumeMuteFeedback(args.Data);
		}

		#endregion

		#endregion

		#region Panel Callback

		private void Subscribe(KrangAtHomeSPlusTouchpanelDevice panel)
		{
			if (panel == null)
				return;

			panel.OnRequestRefresh += PanelOnRequestRefresh;
			panel.OnSetRoomIndex += PanelOnSetRoomIndex;
			panel.OnSetRoomId += PanelOnSetRoomId;
			panel.OnSetAudioSourceIndex += PanelOnSetAudioSourceIndex;
			panel.OnSetAudioSourceId += PanelOnSetAudioSourceId;
			panel.OnSetVideoSourceIndex += PanelOnSetVideoSourceIndex;
			panel.OnSetVideoSourceId += PanelOnSetVideoSourceId;
			panel.OnSetVolumeLevel += PanelOnSetVolumeLevel;
			panel.OnSetVolumeRampUp += PanelOnSetVolumeRampUp;
			panel.OnSetVolumeRampDown += PanelOnSetVolumeRampDown;
			panel.OnSetVolumeRampStop += PanelOnSetVolumeRampStop;
			panel.OnSetVolumeMute += PanelOnSetVolumeMute;
			panel.OnSetVolumeMuteToggle += PanelOnSetVolumeMuteToggle;
		}

		private void Unsubscribe(KrangAtHomeSPlusTouchpanelDevice panel)
		{
			if (panel == null)
				return;

			panel.OnRequestRefresh -= PanelOnRequestRefresh;
			panel.OnSetRoomIndex -= PanelOnSetRoomIndex;
			panel.OnSetRoomId -= PanelOnSetRoomId;
			panel.OnSetAudioSourceIndex -= PanelOnSetAudioSourceIndex;
			panel.OnSetAudioSourceId -= PanelOnSetAudioSourceId;
			panel.OnSetVideoSourceIndex -= PanelOnSetVideoSourceIndex;
			panel.OnSetVideoSourceId -= PanelOnSetVideoSourceId;
			panel.OnSetVolumeLevel -= PanelOnSetVolumeLevel;
			panel.OnSetVolumeRampUp -= PanelOnSetVolumeRampUp;
			panel.OnSetVolumeRampDown -= PanelOnSetVolumeRampDown;
			panel.OnSetVolumeRampStop -= PanelOnSetVolumeRampStop;
			panel.OnSetVolumeMute -= PanelOnSetVolumeMute;
			panel.OnSetVolumeMuteToggle -= PanelOnSetVolumeMuteToggle;
		}

		/// <summary>
		/// Panel requesting a refresh
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PanelOnRequestRefresh(object sender, EventArgs args)
		{
			RaiseRoomList();
			RaiseSourceList();
			RaiseRoomInfo();
		}

		private void PanelOnSetRoomIndex(object sender, UShortEventArgs args)
		{
			SetRoomIndex(args.Data);
		}

		private void PanelOnSetRoomId(object sender, IntEventArgs args)
		{
			SetRoomId(args.Data);
		}

		private void PanelOnSetAudioSourceIndex(object sender, UShortEventArgs args)
		{
			SetAudioSourceIndex(args.Data);
		}

		private void PanelOnSetAudioSourceId(object sender, IntEventArgs args)
		{
			SetSourceId(args.Data, eSourceTypeRouted.Audio);
		}

		private void PanelOnSetVideoSourceIndex(object sender, UShortEventArgs args)
		{
			SetVideoSourceIndex(args.Data);
		}

		private void PanelOnSetVideoSourceId(object sender, IntEventArgs args)
		{
			SetSourceId(args.Data, eSourceTypeRouted.Video);
		}

		private void PanelOnSetVolumeLevel(object sender, FloatEventArgs args)
		{
			IVolumePositionDeviceControl control = m_VolumeControl as IVolumePositionDeviceControl;

			if (control != null)
				control.SetVolumePosition(args.Data);
		}

		private void PanelOnSetVolumeRampUp(object sender, EventArgs args)
		{
			IVolumeRampDeviceControl control = m_VolumeControl as IVolumeRampDeviceControl;

			if (control != null)
				control.VolumeRampUp();
		}

		private void PanelOnSetVolumeRampDown(object sender, EventArgs args)
		{
			IVolumeRampDeviceControl control = m_VolumeControl as IVolumeRampDeviceControl;

			if (control != null)
				control.VolumeRampDown();
		}

		private void PanelOnSetVolumeRampStop(object sender, EventArgs args)
		{
			IVolumeRampDeviceControl control = m_VolumeControl as IVolumeRampDeviceControl;

			if (control != null)
				control.VolumeRampStop();
		}

		private void PanelOnSetVolumeMute(object sender, BoolEventArgs args)
		{
			IVolumeMuteDeviceControl control = m_VolumeControl as IVolumeMuteDeviceControl;

			if (control != null)
				control.SetVolumeMute(args.Data);
		}

		private void PanelOnSetVolumeMuteToggle(object sender, EventArgs args)
		{
			IVolumeMuteBasicDeviceControl control = m_VolumeControl as IVolumeMuteBasicDeviceControl;

			if (control != null)
				control.VolumeMuteToggle();
		}

		#endregion
	}
}
