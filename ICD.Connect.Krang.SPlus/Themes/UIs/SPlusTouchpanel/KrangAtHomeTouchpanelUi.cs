using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls.Mute;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.Device;
using ICD.Connect.Settings.Originators;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusTouchpanel
{
	public sealed class KrangAtHomeTouchpanelUi : IKrangAtHomeUserInterface
	{
		private const int INDEX_NOT_FOUND = -1;
		private const int INDEX_OFF = -1;
		private const int INDEX_START = 0;

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
		private BiDictionary<int, IKrangAtHomeRoom> m_RoomListBiDictionary;

		private BiDictionary<int, IKrangAtHomeSourceBase> m_SourceListAudioBiDictionary;

		private BiDictionary<int, IKrangAtHomeSourceBase> m_SourceListVideoBiDictionary;

		#endregion

		#region Properties

		public KrangAtHomeSPlusTouchpanelDevice Panel {get { return m_Panel; }}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangAtHomeTouchpanelUi(KrangAtHomeTheme theme,KrangAtHomeSPlusTouchpanelDevice panel)
		{
			m_RoomListBiDictionary = new BiDictionary<int, IKrangAtHomeRoom>();
			m_SourceListAudioBiDictionary = new BiDictionary<int, IKrangAtHomeSourceBase>();
			m_SourceListVideoBiDictionary = new BiDictionary<int, IKrangAtHomeSourceBase>();

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
		private void SetRoomId(int roomId)
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
		private void SetRoomIndex(int roomIndex)
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

		private void SetVideoSourceIndex(int sourceIndex)
		{
			// sourceIndex of -1 is used for "Off" - pass it along w/o lookup
			if (sourceIndex == INDEX_OFF)
			{
				SetSource(null, eSourceTypeRouted.AudioVideo);
				return;
			}

			IKrangAtHomeSourceBase source;

			if (!m_SourceListVideoBiDictionary.TryGetValue(sourceIndex, out source))
				return;

			SetSource(source, eSourceTypeRouted.AudioVideo);
		}

		private void SetAudioSourceIndex(int sourceIndex)
		{
			// sourceIndex of -1 is used for "Off" - pass it along w/o lookup
			if (sourceIndex == INDEX_OFF)
			{
				SetSource(null, eSourceTypeRouted.Audio);
				return;
			}

			IKrangAtHomeSourceBase source;

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

			IKrangAtHomeSource source = GetSourceId(sourceId);

			if (source == null)
				return;

			SetSource(source, type);
		}

		/// <summary>
		/// Sets the selected source for the room
		/// </summary>
		/// <param name="source"></param>
		/// <param name="type"></param>
		private void SetSource(IKrangAtHomeSourceBase source, eSourceTypeRouted type)
		{
			if (m_Room == null)
				return;

			if (source == null)
			{
				m_Room.SetSource(null, type);
				return;
			}

			m_Room.SetSource(source.GetSource(), type);
		}

		private IKrangAtHomeSource GetSourceId(int id)
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
				SetVolumeControl(m_Room.ActiveVolumeControl);
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

			int index;
			if (!m_RoomListBiDictionary.TryGetKey(m_Room, out index))
				index = INDEX_NOT_FOUND;

			m_Panel.SetRoomInfo(m_Room, index);

			RaiseSourceList();
		}

		private void RaiseRoomList()
		{
			BiDictionary<int, IKrangAtHomeRoom> roomListDictionary = new BiDictionary<int, IKrangAtHomeRoom>();

			ushort counter = INDEX_START;
			foreach (IKrangAtHomeRoom room in m_Theme.Core.Originators.GetChildren<IKrangAtHomeRoom>())
			{
				roomListDictionary.Add(counter, room);
				counter++;
			}

			m_Panel.SetRoomList(ConvertToRoomInfo(roomListDictionary));

			m_RoomListBiDictionary = roomListDictionary;

			RaiseRoomInfo();
		}

		private void RaiseSourceList()
		{
			BiDictionary<int, IKrangAtHomeSourceBase> sourceListAudioBiDictionary = new BiDictionary<int, IKrangAtHomeSourceBase>();
			BiDictionary<int, IKrangAtHomeSourceBase> sourceListVideoBiDictionary = new BiDictionary<int, IKrangAtHomeSourceBase>();

			//ushort[] indexArray = {INDEX_START, INDEX_START};
			ushort audioListIndexCounter = INDEX_START;
			ushort videoListIndexCounter = INDEX_START;

			IEnumerable<IKrangAtHomeSourceBase> sources =
				m_Room == null
					? Enumerable.Empty<IKrangAtHomeSourceBase>()
					: m_Room.Originators.GetInstancesRecursive<IKrangAtHomeSourceBase>();

			foreach (IKrangAtHomeSourceBase source in sources)
			{

				if (source.SourceVisibility.HasFlag(KrangAtHomeSource.eSourceVisibility.Audio))
				{
					sourceListAudioBiDictionary.Add(audioListIndexCounter, source);
					audioListIndexCounter++;
				}

				if (source.SourceVisibility.HasFlag(KrangAtHomeSource.eSourceVisibility.Video))
				{
					sourceListVideoBiDictionary.Add(videoListIndexCounter, source);
					videoListIndexCounter++;
				}

			}
			m_Panel.SetAudioSourceList(ConvertToSourceInfo(sourceListAudioBiDictionary));
			m_Panel.SetVideoSourceList(ConvertToSourceInfo(sourceListVideoBiDictionary));

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
		private void RoomOnActiveSourcesChange(object sender, EventArgs eventArgs)
		{
			IKrangAtHomeSource source = m_Room == null ? null : m_Room.GetSource();

			int index;
			if (source == null)
				index = -1;
			else if (!m_SourceListVideoBiDictionary.TryGetKey(source, out index))
				index = -1;

			m_Panel.SetSourceInfo(source, index, eSourceTypeRouted.Video);
		}

		private void RoomOnActiveVolumeControlChanged(object sender, GenericEventArgs<IVolumeDeviceControl> args)
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
			// Test for ActiveVolumeControl being Null
			if (volumeDevice == null)
			{
				m_Panel.SetVolumeAvaliableControls(eVolumeLevelAvailableControl.None, eVolumeMuteAvailableControl.None);
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
			eVolumeLevelAvailableControl volumeAvailableControl = eVolumeLevelAvailableControl.None;
			float volumeLevelFeedback = 0;
			if (volumePositionDevice != null)
			{
				volumeAvailableControl = eVolumeLevelAvailableControl.Position;
				volumeLevelFeedback = volumePositionDevice.VolumePosition;
			}
			else if (volumeRampDevice != null)
				volumeAvailableControl = eVolumeLevelAvailableControl.Ramp;

			//Figure out mute control and feedback
			eVolumeMuteAvailableControl muteAvailableControl = eVolumeMuteAvailableControl.None;
			bool muteStateFeedback = false;
			if (muteFeedbackDevice != null)
			{
				muteAvailableControl = eVolumeMuteAvailableControl.Feedback;
				muteStateFeedback = muteFeedbackDevice.VolumeIsMuted;
			}
			else if (muteDiscreteDevice != null)
				muteAvailableControl = eVolumeMuteAvailableControl.Discrete;
			else if (muteBasicDevice != null)
				muteAvailableControl = eVolumeMuteAvailableControl.Toggle;

			// Send Feedback to Panel
			m_Panel.SetVolumeAvaliableControls(volumeAvailableControl, muteAvailableControl);
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

		private void PanelOnSetRoomIndex(object sender, IntEventArgs args)
		{
			SetRoomIndex(args.Data);
		}

		private void PanelOnSetRoomId(object sender, IntEventArgs args)
		{
			SetRoomId(args.Data);
		}

		private void PanelOnSetAudioSourceIndex(object sender, IntEventArgs args)
		{
			SetAudioSourceIndex(args.Data);
		}

		private void PanelOnSetAudioSourceId(object sender, IntEventArgs args)
		{
			SetSourceId(args.Data, eSourceTypeRouted.Audio);
		}

		private void PanelOnSetVideoSourceIndex(object sender, IntEventArgs args)
		{
			SetVideoSourceIndex(args.Data);
		}

		private void PanelOnSetVideoSourceId(object sender, IntEventArgs args)
		{
			SetSourceId(args.Data, eSourceTypeRouted.AudioVideo);
		}

		private void PanelOnSetVolumeLevel(object sender, FloatEventArgs args)
		{
			IVolumePositionDeviceControl control = m_VolumeControl as IVolumePositionDeviceControl;

			if (control != null)
				control.SetVolumePosition(args.Data);
		}

		private void PanelOnSetVolumeRampUp(object sender, EventArgs args)
		{
			IVolumeLevelDeviceControl controlLvl = m_VolumeControl as IVolumeLevelDeviceControl;
			IVolumeRampDeviceControl control = m_VolumeControl as IVolumeRampDeviceControl;
			
			if (controlLvl != null)
				controlLvl.VolumePositionRampUp(0.05f);
			else if (control != null)
				control.VolumeRampUp();
		}

		private void PanelOnSetVolumeRampDown(object sender, EventArgs args)
		{
			IVolumeLevelDeviceControl controlLvl = m_VolumeControl as IVolumeLevelDeviceControl;
			IVolumeRampDeviceControl control = m_VolumeControl as IVolumeRampDeviceControl;

			if (controlLvl != null)
				controlLvl.VolumePositionRampDown(0.05f);
			else if (control != null)
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

		private static List<RoomInfo> ConvertToRoomInfo(BiDictionary<int, IKrangAtHomeRoom> list)
		{
			List<RoomInfo> returnList = new List<RoomInfo>();
			foreach (var kvp in list)
			{
				returnList.Insert(kvp.Key,new RoomInfo(kvp.Value));
			}

			return returnList;
		}

		private static List<SourceBaseInfo> ConvertToSourceInfo(BiDictionary<int, IKrangAtHomeSourceBase> list)
		{
			List<SourceBaseInfo> returnList = new List<SourceBaseInfo>();
			foreach (var kvp in list)
			{
				returnList.Insert(kvp.Key, new SourceBaseInfo(kvp.Value));
			}

			return returnList;
		}
	}
}
