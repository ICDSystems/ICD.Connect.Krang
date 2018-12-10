using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices.Simpl;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Devices
{

	public enum eVolumeLevelControlsAvaliable
	{
		None,
		Ramp,
		Position
	}

	public enum eVolumeMuteControlsAvaliable
	{
		None,
		Toggle,
		Discrete,
		Feedback
	}

	public sealed class KrangAtHomeSPlusTouchpanelDevice: AbstractSimplDevice<KrangAtHomeSPlusTouchpanelDeviceSettings>
	{

		#region Events to Shim

		public event EventHandler<GenericEventArgs<IEnumerable<KeyValuePair<ushort, IKrangAtHomeRoom>>>> OnRoomListUpdate;

		public event EventHandler<RoomInfoEventArgs> OnRoomInfoUpdate;

		public event EventHandler<GenericEventArgs<IEnumerable<KeyValuePair<ushort, ISimplSource>>>> OnAudioSourceListUpdate;

		public event EventHandler<GenericEventArgs<IEnumerable<KeyValuePair<ushort, ISimplSource>>>> OnVideoSourceListUpdate;

		public event EventHandler<SourceInfoEventArgs> OnSourceInfoUpdate;

		public event EventHandler<FloatEventArgs> OnVolumeLevelFeedbackUpdate;

		public event EventHandler<BoolEventArgs> OnVolumeMuteFeedbackUpdate;

		#endregion

		#region Events to Ui

		public event EventHandler OnRequestRefresh;

		public event EventHandler<UShortEventArgs> OnSetRoomIndex;

		public event EventHandler<IntEventArgs> OnSetRoomId;

		public event EventHandler<UShortEventArgs> OnSetAudioSourceIndex;

		public event EventHandler<IntEventArgs> OnSetAudioSourceId;

		public event EventHandler<UShortEventArgs> OnSetVideoSourceIndex;

		public event EventHandler<IntEventArgs> OnSetVideoSourceId;

		public event EventHandler<FloatEventArgs> OnSetVolumeLevel;

		public event EventHandler OnSetVolumeRampUp;

		public event EventHandler OnSetVolumeRampDown;

		public event EventHandler OnSetVolumeRampStop;

		public event EventHandler<BoolEventArgs> OnSetVolumeMute;

		public event EventHandler OnSetVolumeMuteToggle;

		#endregion

		#region Methods Called from Shim

		public void RequestDeviceRefresh()
		{
			OnRequestRefresh.Raise(this);
		}

		public void SetRoomIndex(ushort index)
		{
			OnSetRoomIndex.Raise(this, new UShortEventArgs(index));
		}

		public void SetRoomId(int id)
		{
			OnSetRoomId.Raise(this, new IntEventArgs(id));
		}

		public void SetAudioSourceIndex(ushort index)
		{
			OnSetAudioSourceIndex.Raise(this, new UShortEventArgs(index));
		}

		public void SetAudioSourcdId(int id)
		{
			OnSetAudioSourceId.Raise(this, new IntEventArgs(id));
		}

		public void SetVideoSourceIndex(ushort index)
		{
			OnSetVideoSourceIndex.Raise(this, new UShortEventArgs(index));
		}

		public void SetVideoSourcdId(int id)
		{
			OnSetVideoSourceId.Raise(this, new IntEventArgs(id));
		}

		public void SetVolumeLevel(float volumeLevel)
		{
			OnSetVolumeLevel.Raise(this, new FloatEventArgs(volumeLevel));
		}

		public void SetVolumeRampUp()
		{
			OnSetVolumeRampUp.Raise(this);
		}

		public void SetVolumeRampDown()
		{
			OnSetVolumeRampDown.Raise(this);
		}

		public void SetVolumeRampStop()
		{
			OnSetVolumeRampStop.Raise(this);
		}

		public void SetVolumeMute(bool state)
		{
			OnSetVolumeMute.Raise(this, new BoolEventArgs(state));
		}

		public void SetVolumeMuteToggle()
		{
			OnSetVolumeMuteToggle.Raise(this);
		}

		#endregion

		#region Methods Called from Ui

		/// <summary>
		/// Sets the room info via the delegate
		/// </summary>
		/// <param name="room"></param>
		/// <param name="index"></param>
		internal void SetRoomInfo(IKrangAtHomeRoom room, ushort index)
		{
			OnRoomInfoUpdate.Raise(this, new RoomInfoEventArgs(room, index));
		}

		/// <summary>
		/// Updates the room list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="roomList"></param>
		internal void SetRoomList(IEnumerable<KeyValuePair<ushort, IKrangAtHomeRoom>> roomList)
		{
			OnRoomListUpdate.Raise(this, new GenericEventArgs<IEnumerable<KeyValuePair<ushort, IKrangAtHomeRoom>>>(roomList));
		}

		/// <summary>
		/// Sets the source info via the delegate
		/// </summary>
		/// <param name="source"></param>
		/// <param name="sourceIndex"></param>
		/// <param name="sourceTypeRouted"></param>
		internal void SetSourceInfo(ISimplSource source, ushort sourceIndex, eSourceTypeRouted sourceTypeRouted)
		{
			OnSourceInfoUpdate.Raise(this, new SourceInfoEventArgs(source, sourceIndex, sourceTypeRouted));
		}

		/// <summary>
		/// Updates the audio source list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="sourceList"></param>
		internal void SetAudioSourceList(IEnumerable<KeyValuePair<ushort, ISimplSource>> sourceList)
		{
			OnAudioSourceListUpdate.Raise(this, new GenericEventArgs<IEnumerable<KeyValuePair<ushort, ISimplSource>>>(sourceList));
		}

		/// <summary>
		/// Updates the video source list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="sourceList"></param>
		internal void SetVideoSourceList(IEnumerable<KeyValuePair<ushort, ISimplSource>> sourceList)
		{
			OnVideoSourceListUpdate.Raise(this, new GenericEventArgs<IEnumerable<KeyValuePair<ushort, ISimplSource>>>(sourceList));
		}

		internal void SetVolumeLevelFeedback(float volume)
		{
			OnVolumeLevelFeedbackUpdate.Raise(this, new FloatEventArgs(volume));
		}

		internal void SetVolumeMuteFeedback(bool mute)
		{
			OnVolumeMuteFeedbackUpdate.Raise(this, new BoolEventArgs(mute));
		}

		internal void SetVolumeAvaliableControls(eVolumeLevelControlsAvaliable levelControls,
		                                         eVolumeMuteControlsAvaliable muteControls)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}