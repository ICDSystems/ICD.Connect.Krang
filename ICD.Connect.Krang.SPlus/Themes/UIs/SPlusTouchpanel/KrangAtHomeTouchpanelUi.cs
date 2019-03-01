using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls.Mute;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Device;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusTouchpanel
{
	public sealed class KrangAtHomeTouchpanelUi : AbstractKrangAtHomeUi<KrangAtHomeSPlusTouchpanelDevice>
	{
		private const int INDEX_NOT_FOUND = -1;
		private const int INDEX_OFF = -1;
		private const int INDEX_START = 0;

		#region Fields

		/// <summary>
		/// List of rooms and indexes
		/// </summary>
		private BiDictionary<int, IKrangAtHomeRoom> m_RoomListBiDictionary;

		private BiDictionary<int, IKrangAtHomeSourceBase> m_SourceListAudioBiDictionary;

		private BiDictionary<int, IKrangAtHomeSourceBase> m_SourceListVideoBiDictionary;

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangAtHomeTouchpanelUi(KrangAtHomeTheme theme,KrangAtHomeSPlusTouchpanelDevice uiDevice) : base(theme, uiDevice)
		{
			m_RoomListBiDictionary = new BiDictionary<int, IKrangAtHomeRoom>();
			m_SourceListAudioBiDictionary = new BiDictionary<int, IKrangAtHomeSourceBase>();
			m_SourceListVideoBiDictionary = new BiDictionary<int, IKrangAtHomeSourceBase>();

			Subscribe(UiDevice);
		}

		#region Methods


		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();
			
			m_RoomListBiDictionary.Clear();
			m_SourceListAudioBiDictionary.Clear();
			m_SourceListVideoBiDictionary.Clear();
		}
		
		#endregion

		#region Room Select

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

		#endregion

		#region Private Methods

		protected override void RaiseRoomInfo()
		{
			if (Room == null)
			{
				UiDevice.SetRoomInfo(null, INDEX_NOT_FOUND);
				return;
			}

			int index;
			if (!m_RoomListBiDictionary.TryGetKey(Room, out index))
				index = INDEX_NOT_FOUND;

			UiDevice.SetRoomInfo(Room, index);

			RaiseSourceList();
		}

		private void RaiseRoomList()
		{
			BiDictionary<int, IKrangAtHomeRoom> roomListDictionary = new BiDictionary<int, IKrangAtHomeRoom>();

			ushort counter = INDEX_START;
			foreach (IKrangAtHomeRoom room in Theme.Core.Originators.GetChildren<IKrangAtHomeRoom>())
			{
				roomListDictionary.Add(counter, room);
				counter++;
			}

			UiDevice.SetRoomList(ConvertToRoomInfo(roomListDictionary));

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
				Room == null
					? Enumerable.Empty<IKrangAtHomeSourceBase>()
					: Room.Originators.GetInstancesRecursive<IKrangAtHomeSourceBase>().OrderBy(s => s.Order);

			foreach (IKrangAtHomeSourceBase source in sources)
			{

				if (source.SourceVisibility.HasFlag(eSourceVisibility.Audio))
				{
					sourceListAudioBiDictionary.Add(audioListIndexCounter, source);
					audioListIndexCounter++;
				}

				if (source.SourceVisibility.HasFlag(eSourceVisibility.Video))
				{
					sourceListVideoBiDictionary.Add(videoListIndexCounter, source);
					videoListIndexCounter++;
				}

			}
			UiDevice.SetAudioSourceList(ConvertToSourceInfo(sourceListAudioBiDictionary));
			UiDevice.SetVideoSourceList(ConvertToSourceInfo(sourceListVideoBiDictionary));

			m_SourceListAudioBiDictionary = sourceListAudioBiDictionary;
			m_SourceListVideoBiDictionary = sourceListVideoBiDictionary;

			RaiseSourceInfo();
		}

		private void RaiseSourceInfo()
		{
			IKrangAtHomeSource source = Room == null ? null : Room.GetSource();
			IEnumerable<IKrangAtHomeSourceBase> sourcesBase = Room == null ? null : Room.GetSourcesBase();

			int index;
			if (source == null)
			{
				UiDevice.SetSourceInfo(null, INDEX_NOT_FOUND, INDEX_NOT_FOUND);
				return;
			}

			// Video vs Audio
			//Todo: Add Better Determination of Audio vs Video
			if (EnumUtils.HasFlag(source.SourceVisibility, eSourceVisibility.Video))
			{
				// Try to get the specific source first
				if (m_SourceListVideoBiDictionary.TryGetKey(source, out index))
				{
					UiDevice.SetSourceInfo(source, INDEX_NOT_FOUND, index);
					return;
				}
				// Try to find a match in source groups
				if (sourcesBase != null)
				{
					if (sourcesBase.Any(sourceBase => m_SourceListVideoBiDictionary.TryGetKey(sourceBase, out index)))
					{
						UiDevice.SetSourceInfo(source, INDEX_NOT_FOUND, index);
						return;
					}
				}
				//return not found
				UiDevice.SetSourceInfo(source, INDEX_NOT_FOUND, INDEX_NOT_FOUND);
			}
			else
			{
				// Try to get the specific source first

				if (m_SourceListAudioBiDictionary.TryGetKey(source, out index))
				{
					UiDevice.SetSourceInfo(source, index, INDEX_NOT_FOUND);
					return;
				}
				// Try to find a match in source groups
				if (sourcesBase != null)
				{
					if (sourcesBase.Any(sourceBase => m_SourceListAudioBiDictionary.TryGetKey(sourceBase, out index)))
					{
						UiDevice.SetSourceInfo(source, index, INDEX_NOT_FOUND);
						return;
					}
				}
				//return not found
				UiDevice.SetSourceInfo(source, INDEX_NOT_FOUND, INDEX_NOT_FOUND);
			}
		}

		#endregion

		#region Room Callbacks

		/// <summary>
		/// Raised when source/s become actively/inactively routed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		protected override void RoomOnActiveSourcesChange(object sender, EventArgs eventArgs)
		{
			RaiseSourceInfo();	
		}

		#endregion

		#region VolumeDeviceControl

		protected override void InstantiateVolumeControl(IVolumeDeviceControl volumeDevice)
		{
			// Test for ActiveVolumeControl being Null
			if (volumeDevice == null)
			{
				UiDevice.SetVolumeAvaliableControls(eVolumeLevelAvailableControl.None, eVolumeMuteAvailableControl.None);
				UiDevice.SetVolumeLevelFeedback(0);
				UiDevice.SetVolumeMuteFeedback(false);
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
			UiDevice.SetVolumeAvaliableControls(volumeAvailableControl, muteAvailableControl);
			UiDevice.SetVolumeLevelFeedback(volumeLevelFeedback);
			UiDevice.SetVolumeMuteFeedback(muteStateFeedback);
		}

		#region VolumePositionDeviceControl

		protected override void SubscribeVolumePositionDeviceControl(IVolumePositionDeviceControl control)
		{
			if (control == null)
				return;

			control.OnVolumeChanged += ControlOnVolumeChanged;
		}

		protected override void UnsubscribeVolumePositionDeviceControl(IVolumePositionDeviceControl control)
		{
			if (control == null)
				return;

			control.OnVolumeChanged -= ControlOnVolumeChanged;
		}

		private void ControlOnVolumeChanged(object sender, VolumeDeviceVolumeChangedEventArgs args)
		{
			UiDevice.SetVolumeLevelFeedback(args.VolumePosition);
		}

		#endregion

		#region MuteFeedbackDeviceControl

		protected override void SubscribeVolumeMuteFeedbackDeviceControl(IVolumeMuteFeedbackDeviceControl control)
		{
			if (control == null)
				return;

			control.OnMuteStateChanged += ControlOnMuteStateChanged;
		}

		protected override void UnsubscribeVolumeMuteFeedbackDeviceControl(IVolumeMuteFeedbackDeviceControl control)
		{
			if (control == null)
				return;

			control.OnMuteStateChanged -= ControlOnMuteStateChanged;
		}

		private void ControlOnMuteStateChanged(object sender, BoolEventArgs args)
		{
			UiDevice.SetVolumeMuteFeedback(args.Data);
		}

		#endregion

		#endregion

		#region Panel Callback

		protected override void Subscribe(KrangAtHomeSPlusTouchpanelDevice panel)
		{
			base.Subscribe(panel);

			if (panel == null)
				return;

			panel.OnRequestRefresh += PanelOnRequestRefresh;
			panel.OnSetRoomIndex += PanelOnSetRoomIndex;
			panel.OnSetAudioSourceIndex += PanelOnSetAudioSourceIndex;
			panel.OnSetVideoSourceIndex += PanelOnSetVideoSourceIndex;
		}

		protected override void Unsubscribe(KrangAtHomeSPlusTouchpanelDevice panel)
		{
			base.Unsubscribe(panel);

			if (panel == null)
				return;

			panel.OnRequestRefresh -= PanelOnRequestRefresh;
			panel.OnSetRoomIndex -= PanelOnSetRoomIndex;
			panel.OnSetAudioSourceIndex -= PanelOnSetAudioSourceIndex;
			panel.OnSetVideoSourceIndex -= PanelOnSetVideoSourceIndex;
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

		private void PanelOnSetAudioSourceIndex(object sender, IntEventArgs args)
		{
			SetAudioSourceIndex(args.Data);
		}

		private void PanelOnSetVideoSourceIndex(object sender, IntEventArgs args)
		{
			SetVideoSourceIndex(args.Data);
		}

		#endregion

		private static List<RoomInfo> ConvertToRoomInfo(IEnumerable<KeyValuePair<int, IKrangAtHomeRoom>> list)
		{
			List<RoomInfo> returnList = new List<RoomInfo>();
			foreach (var kvp in list)
			{
				returnList.Insert(kvp.Key,new RoomInfo(kvp.Value));
			}

			return returnList;
		}

		private static List<SourceBaseInfo> ConvertToSourceInfo(IEnumerable<KeyValuePair<int, IKrangAtHomeSourceBase>> list)
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
