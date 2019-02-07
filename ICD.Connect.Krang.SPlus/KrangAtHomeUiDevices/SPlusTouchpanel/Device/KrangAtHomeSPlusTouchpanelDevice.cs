using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Device
{
	/// <summary>
	/// What volume level controls are avaliable
	/// Backing values used by S+
	/// </summary>
	public enum eVolumeLevelAvailableControl
	{
		None = 0,
		Ramp = 1,
		Position = 2
	}

	/// <summary>
	/// What volume mute controls are avalialbe
	/// Backing values used by S+
	/// </summary>
	public enum eVolumeMuteAvailableControl
	{
		None = 0,
		Toggle = 1,
		Discrete = 2,
		Feedback = 3
	}

	public sealed class KrangAtHomeSPlusTouchpanelDevice : AbstractKrangAtHomeUiDevice<KrangAtHomeSPlusTouchpanelDeviceSettings>, IKrangAtHomeSPlusTouchpanelDeviceShimmable
	{

		#region Events to Shim

		public event EventHandler<RoomListEventArgs> OnRoomListUpdate;

		public event EventHandler<RoomSelectedEventArgs> OnRoomSelectedUpdate;

		public event EventHandler<AudioSourceBaseListEventArgs> OnAudioSourceListUpdate;

		public event EventHandler<VideoSourceBaseListEventArgs> OnVideoSourceListUpdate;

		public event EventHandler<SourceSelectedEventArgs> OnSourceSelectedUpdate;

		public event EventHandler<VolumeLevelFeedbackEventArgs> OnVolumeLevelFeedbackUpdate;

		public event EventHandler<VolumeMuteFeedbackEventArgs> OnVolumeMuteFeedbackUpdate;

		public event EventHandler<VolumeAvailableControlEventArgs> OnVolumeAvailableControlUpdate; 

		#endregion

		#region Events to Ui

		public event EventHandler OnRequestRefresh;

		public event EventHandler<IntEventArgs> OnSetRoomIndex;

		public event EventHandler<IntEventArgs> OnSetAudioSourceIndex;

		public event EventHandler<IntEventArgs> OnSetVideoSourceIndex;

		#endregion

		#region Methods Called from Shim

		public void RequestDeviceRefresh()
		{
			OnRequestRefresh.Raise(this);
		}

		public void SetRoomIndex(int index)
		{
			OnSetRoomIndex.Raise(this, new IntEventArgs(index));
		}

		public void SetAudioSourceIndex(int index)
		{
			OnSetAudioSourceIndex.Raise(this, new IntEventArgs(index));
		}

		public void SetVideoSourceIndex(int index)
		{
			OnSetVideoSourceIndex.Raise(this, new IntEventArgs(index));
		}

		#endregion

		#region Methods Called from Ui

		/// <summary>
		/// Sets the room info via the delegate
		/// </summary>
		/// <param name="room"></param>
		/// <param name="index"></param>
		internal void SetRoomInfo(IKrangAtHomeRoom room, int index)
		{
			OnRoomSelectedUpdate.Raise(this, new RoomSelectedEventArgs(room, index));
		}

		/// <summary>
		/// Updates the room list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="roomList"></param>
		internal void SetRoomList(List<RoomInfo> roomList)
		{
			OnRoomListUpdate.Raise(this, new RoomListEventArgs(roomList));
		}

		/// <summary>
		/// Sets the source info via the delegate
		/// </summary>
		/// <param name="source"></param>
		/// <param name="sourceIndex"></param>
		/// <param name="sourceTypeRouted"></param>
		internal void SetSourceInfo(IKrangAtHomeSource source, int sourceIndex, eSourceTypeRouted sourceTypeRouted)
		{
			OnSourceSelectedUpdate.Raise(this, new SourceSelectedEventArgs(source, sourceIndex, sourceTypeRouted));
		}

		/// <summary>
		/// Updates the audio source list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="sourceList"></param>
		internal void SetAudioSourceList(List<SourceBaseInfo> sourceList)
		{
			OnAudioSourceListUpdate.Raise(this, new AudioSourceBaseListEventArgs(sourceList));
		}

		/// <summary>
		/// Updates the video source list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="sourceList"></param>
		internal void SetVideoSourceList(List<SourceBaseInfo> sourceList)
		{
			OnVideoSourceListUpdate.Raise(this, new VideoSourceBaseListEventArgs(sourceList));
		}

		internal void SetVolumeLevelFeedback(float volume)
		{
			OnVolumeLevelFeedbackUpdate.Raise(this, new VolumeLevelFeedbackEventArgs(volume));
		}

		internal void SetVolumeMuteFeedback(bool mute)
		{
			OnVolumeMuteFeedbackUpdate.Raise(this, new VolumeMuteFeedbackEventArgs(mute));
		}

		internal void SetVolumeAvaliableControls(eVolumeLevelAvailableControl levelControl,
		                                         eVolumeMuteAvailableControl muteControl)
		{
			OnVolumeAvailableControlUpdate.Raise(this, new VolumeAvailableControlEventArgs(levelControl, muteControl));
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("RefreshPanel", "Requeust the UI to refresh all data on the panel", () => OnRequestRefresh.Raise(this));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}