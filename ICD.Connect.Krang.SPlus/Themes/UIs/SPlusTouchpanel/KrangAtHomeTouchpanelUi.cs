using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusTouchpanel
{
	public sealed class KrangAtHomeTouchpanelUi : AbstractKrangAtHomeUi<IKrangAtHomeSPlusTouchpanelDevice>
	{
		private const int INDEX_NOT_FOUND = -1;
		private const int INDEX_OFF = -1;
		private const int INDEX_START = 0;

		#region Fields

		/// <summary>
		/// List of rooms and indexes
		/// </summary>
		private BiDictionary<int, IKrangAtHomeRoom> m_RoomListBiDictionary;

		/// <summary>
		/// List of audio sources and indexes
		/// </summary>
		private BiDictionary<int, IKrangAtHomeSourceBase> m_SourceListAudioBiDictionary;

		/// <summary>
		/// List of video sources and indexes
		/// </summary>
		private BiDictionary<int, IKrangAtHomeSourceBase> m_SourceListVideoBiDictionary;

		/// <summary>
		/// The current source type
		/// Used to clear feedback icons
		/// </summary>
		private eSourceTypeRouted m_SourceFeedbackType;

		/// <summary>
		/// The current source index
		/// Used to clear feedback icons
		/// </summary>
		private int m_SourceFeedbackIndex;

		#endregion


		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangAtHomeTouchpanelUi(KrangAtHomeTheme theme,KrangAtHomeSPlusTouchpanelDevice uiDevice) : base(theme, uiDevice)
		{
			m_RoomListBiDictionary = new BiDictionary<int, IKrangAtHomeRoom>();
			m_SourceListAudioBiDictionary = new BiDictionary<int, IKrangAtHomeSourceBase>();
			m_SourceListVideoBiDictionary = new BiDictionary<int, IKrangAtHomeSourceBase>();
			m_SourceFeedbackType = eSourceTypeRouted.None;
			m_SourceFeedbackIndex = INDEX_NOT_FOUND;

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

		/// <summary>
		/// Set the video source based on the index of the source list on the UI Device
		/// </summary>
		/// <param name="sourceIndex"></param>
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

		/// <summary>
		/// Set the audio source based on the index of the source list on the UI Device
		/// </summary>
		/// <param name="sourceIndex"></param>
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

		/// <summary>
		/// Updates the UI device with the current room info (including source lists)
		/// </summary>
		protected override void RaiseRoomInfo()
		{
			if (Room == null)
			{
				UiDevice.SetRoomInfo(new RoomSelected(INDEX_NOT_FOUND));
				return;
			}

			int index;
			if (!m_RoomListBiDictionary.TryGetKey(Room, out index))
				index = INDEX_NOT_FOUND;

			UiDevice.SetRoomInfo(new RoomSelected(Room, index));

			RaiseSourceList();
		}

		/// <summary>
		/// Updates the UI Device with the current room list, and updates the current room/source/etc
		/// </summary>
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

		/// <summary>
		/// Updates the UI Device with the current audio and video source lists
		/// </summary>
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
			m_SourceListAudioBiDictionary = sourceListAudioBiDictionary;
			m_SourceListVideoBiDictionary = sourceListVideoBiDictionary;

			int index, audioSourceIndex, videoSourceIndex;
			eSourceTypeRouted type;

			IKrangAtHomeSource activeSource = GetActiveSourceIndex(out index, out type);

			if (type.HasFlag(eSourceTypeRouted.Video))
			{
				audioSourceIndex = INDEX_NOT_FOUND;
				videoSourceIndex = index;
			}
			else
			{
				audioSourceIndex = index;
				videoSourceIndex = INDEX_NOT_FOUND;
			}

			UiDevice.SetAudioSourceList(ConvertToSourceInfo(sourceListAudioBiDictionary,audioSourceIndex));
			UiDevice.SetVideoSourceList(ConvertToSourceInfo(sourceListVideoBiDictionary,videoSourceIndex));

			RaiseSourceInfo(activeSource, index, type);
		}

		/// <summary>
		/// Sends the current Source Info to the UI Device
		/// </summary>
		private void RaiseSourceInfo()
		{
			int index;
			eSourceTypeRouted type;

			IKrangAtHomeSource source = GetActiveSourceIndex(out index, out type);

			RaiseSourceInfo(source, index, type);
		}

		/// <summary>
		/// Sends the specified Source Info to the UI Device
		/// Also updates the list to un-select the previous source and select the new source
		/// </summary>
		/// <param name="source"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		private void RaiseSourceInfo(IKrangAtHomeSource source, int index, eSourceTypeRouted type)
		{
			// Need to update the selected icon state also
			if (type != m_SourceFeedbackType || index != m_SourceFeedbackIndex)
			{
				ClearSourceIconFeedback();
				SetSourceIconFeedback(index, type);
			}

			if (type.HasFlag(eSourceTypeRouted.Video))
				UiDevice.SetSourceInfo(new SourceSelected(source, INDEX_NOT_FOUND, index));
			else
				UiDevice.SetSourceInfo(new SourceSelected(source, index, INDEX_NOT_FOUND));
		}

		/// <summary>
		/// Sets the source icon feedback for the specified source on the specified list
		/// </summary>
		/// <param name="index"></param>
		/// <param name="type"></param>
		private void SetSourceIconFeedback(int index, eSourceTypeRouted type)
		{
			if (index == INDEX_NOT_FOUND || type == eSourceTypeRouted.None)
			{
				m_SourceFeedbackIndex = INDEX_NOT_FOUND;
				m_SourceFeedbackType = eSourceTypeRouted.None;
				return;
			}

			if (type.HasFlag(eSourceTypeRouted.Video))
			{
				IKrangAtHomeSourceBase sourceBase;
				if (m_SourceListVideoBiDictionary.TryGetValue(index, out sourceBase))
				{
					UiDevice.SetVideoSourceListItem(new SourceBaseListInfo(sourceBase, index, true));
				}
			}
			else
			{
				IKrangAtHomeSourceBase sourceBase;
				if (m_SourceListAudioBiDictionary.TryGetValue(index, out sourceBase))
				{
					UiDevice.SetAudioSourceListItem(new SourceBaseListInfo(sourceBase, index, true));
				}
			}

			m_SourceFeedbackIndex = index;
			m_SourceFeedbackType = type;
		}

		/// <summary>
		/// Clears source icon feedback from the lists, and sets the items back as not selected
		/// </summary>
		private void ClearSourceIconFeedback()
		{
			if (m_SourceFeedbackIndex == INDEX_NOT_FOUND || m_SourceFeedbackType == eSourceTypeRouted.None)
				return;

			
			if (m_SourceFeedbackType.HasFlag(eSourceTypeRouted.Video))
			{
				IKrangAtHomeSourceBase sourceBase;
				if (m_SourceListVideoBiDictionary.TryGetValue(m_SourceFeedbackIndex, out sourceBase))
				{
					UiDevice.SetVideoSourceListItem(new SourceBaseListInfo(sourceBase, m_SourceFeedbackIndex, false));
				}
			}
			else
			{
				IKrangAtHomeSourceBase sourceBase;
				if (m_SourceListAudioBiDictionary.TryGetValue(m_SourceFeedbackIndex, out sourceBase))
				{
					UiDevice.SetAudioSourceListItem(new SourceBaseListInfo(sourceBase, m_SourceFeedbackIndex, false));
				}
			}

			m_SourceFeedbackIndex = INDEX_NOT_FOUND;
			m_SourceFeedbackType = eSourceTypeRouted.None;
		}

		/// <summary>
		/// Gets the index and type of the active source from the source lists
		/// </summary>
		/// <param name="index">index of the source.  INDEX_NOT_FOUND if not in the list</param>
		/// <param name="type">which list the source is in.  None if not found</param>
		/// <returns></returns>
		private IKrangAtHomeSource GetActiveSourceIndex(out int index, out eSourceTypeRouted type)
		{
			index = INDEX_NOT_FOUND;
			type = eSourceTypeRouted.None;

			IKrangAtHomeSource source = Room == null ? null : Room.GetSource();
			IEnumerable<IKrangAtHomeSourceBase> sourcesBase = Room == null ? null : Room.GetSourcesBase();

			if (source == null)
			{
				return null;
			}

			// Video vs Audio
			//Todo: Add Better Determination of Audio vs Video
			if (EnumUtils.HasFlag(source.SourceVisibility, eSourceVisibility.Video))
			{
				type = eSourceTypeRouted.Video;
				// Try to get the specific source first
				if (m_SourceListVideoBiDictionary.TryGetKey(source, out index))
				{
					return source;
				}
				// Try to find a match in source groups
				if (sourcesBase != null)
				{
					int videoIndex = INDEX_NOT_FOUND;
					if (sourcesBase.Any(sourceBase => m_SourceListVideoBiDictionary.TryGetKey(sourceBase, out videoIndex)))
					{
						index = videoIndex;
						return source;
					}
				}
			}
			else
			{
				type = eSourceTypeRouted.Audio;
				// Try to get the specific source first

				if (m_SourceListAudioBiDictionary.TryGetKey(source, out index))
				{
					return source;
				}
				// Try to find a match in source groups
				if (sourcesBase != null)
				{
					int audioIndex = INDEX_NOT_FOUND;
					if (sourcesBase.Any(sourceBase => m_SourceListAudioBiDictionary.TryGetKey(sourceBase, out audioIndex)))
					{
						index = audioIndex;
						return source;
					}
				}
			}

			index = INDEX_NOT_FOUND;
			return source;
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

		#region Panel Callback

		protected override void Subscribe(IKrangAtHomeSPlusTouchpanelDevice panel)
		{
			base.Subscribe(panel);

			if (panel == null)
				return;

			panel.OnRequestRefresh += PanelOnRequestRefresh;
			panel.OnSetRoomIndex += PanelOnSetRoomIndex;
			panel.OnSetAudioSourceIndex += PanelOnSetAudioSourceIndex;
			panel.OnSetVideoSourceIndex += PanelOnSetVideoSourceIndex;
		}

		protected override void Unsubscribe(IKrangAtHomeSPlusTouchpanelDevice panel)
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

		private void PanelOnSetRoomIndex(object sender, SetRoomIndexApiEventArgs args)
		{
			SetRoomIndex(args.Data);
		}

		private void PanelOnSetAudioSourceIndex(object sender, SetAudioSourceIndexApiEventArgs args)
		{
			SetAudioSourceIndex(args.Data);
		}

		private void PanelOnSetVideoSourceIndex(object sender, SetVideoSourceIndexApiEventArgs args)
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

		private static List<SourceBaseListInfo> ConvertToSourceInfo(IEnumerable<KeyValuePair<int, IKrangAtHomeSourceBase>> list, int activeSourceIndex)
		{
			List<SourceBaseListInfo> returnList = new List<SourceBaseListInfo>();
			foreach (var kvp in list)
			{
				returnList.Insert(kvp.Key, new SourceBaseListInfo(kvp.Value, kvp.Key, kvp.Key == activeSourceIndex));
			}

			return returnList;
		}
	}
}
