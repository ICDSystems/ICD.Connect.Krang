using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Controls.Mute;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

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

	public sealed class KrangAtHomeSPlusTouchpanelDevice : AbstractKrangAtHomeUiDevice<KrangAtHomeSPlusTouchpanelDeviceSettings>, IKrangAtHomeSPlusTouchpanelDeviceShimmable, IKrangAtHomeSPlusTouchpanelDevice
	{

		#region Events to Shim

		public event EventHandler<RoomListEventArgs> OnRoomListUpdate;

		public event EventHandler<RoomSelectedEventArgs> OnRoomSelectedUpdate;

		public event EventHandler<AudioSourceBaseListEventArgs> OnAudioSourceListUpdate;

		public event EventHandler<VideoSourceBaseListEventArgs> OnVideoSourceListUpdate;

		public event EventHandler<SourceSelectedEventArgs> OnSourceSelectedUpdate;

		public event EventHandler<AudioSourceBaseListItemEventArgs> OnAudioSourceListItemUpdate;

		public event EventHandler<VideoSourceBaseListItemEventArgs> OnVideoSourceListItemUpdate;

		public event EventHandler<VolumeLevelFeedbackEventArgs> OnVolumeLevelFeedbackUpdate;

		public event EventHandler<VolumeMuteFeedbackEventArgs> OnVolumeMuteFeedbackUpdate;

		public event EventHandler<VolumeAvailableControlEventArgs> OnVolumeAvailableControlUpdate; 

		#endregion

		#region Events to Ui

		public event EventHandler<SetRoomIndexApiEventArgs> OnSetRoomIndex;

		public event EventHandler<SetAudioSourceIndexApiEventArgs> OnSetAudioSourceIndex;

		public event EventHandler<SetVideoSourceIndexApiEventArgs> OnSetVideoSourceIndex;

		#endregion

		#region Methods Called from Shim

		public void SetRoomIndex(int index)
		{
			OnSetRoomIndex.Raise(this, new SetRoomIndexApiEventArgs(index));
		}

		public void SetAudioSourceIndex(int index)
		{
			OnSetAudioSourceIndex.Raise(this, new SetAudioSourceIndexApiEventArgs(index));
		}

		public void SetVideoSourceIndex(int index)
		{
			OnSetVideoSourceIndex.Raise(this, new SetVideoSourceIndexApiEventArgs(index));
		}

		#endregion

		#region Methods Called from Ui

		/// <summary>
		/// Sets the room info via the delegate
		/// </summary>
		/// <param name="roomInfo"></param>
		public void SetRoomInfo(RoomSelected roomInfo)
		{
			OnRoomSelectedUpdate.Raise(this, new RoomSelectedEventArgs(roomInfo));
		}

		/// <summary>
		/// Updates the room list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="roomList"></param>
		public void SetRoomList(List<RoomInfo> roomList)
		{
			OnRoomListUpdate.Raise(this, new RoomListEventArgs(roomList));
		}

		/// <summary>
		/// Sets the source info via the delegate
		/// </summary>
		/// <param name="sourceInfo"></param>
		public void SetSourceInfo(SourceSelected sourceInfo)
		{
			OnSourceSelectedUpdate.Raise(this, new SourceSelectedEventArgs(sourceInfo));
		}


		/// <summary>
		/// Updates a single item on the audio list (for icon in use update, for example)
		/// </summary>
		/// <param name="sourceListItem"></param>
		public void SetAudioSourceListItem(SourceBaseListInfo sourceListItem)
		{
			OnAudioSourceListItemUpdate.Raise(this, new AudioSourceBaseListItemEventArgs(sourceListItem));
		}

		
		/// <summary>
		/// Updates a single item on the video list (for icon in use update, for example)
		/// </summary>
		/// <param name="sourceListItem"></param>
		public void SetVideoSourceListItem(SourceBaseListInfo sourceListItem)
		{
			OnVideoSourceListItemUpdate.Raise(this, new VideoSourceBaseListItemEventArgs(sourceListItem));
		}

		/// <summary>
		/// Updates the audio source list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="sourceList"></param>
		public void SetAudioSourceList(List<SourceBaseListInfo> sourceList)
		{
			OnAudioSourceListUpdate.Raise(this, new AudioSourceBaseListEventArgs(sourceList));
		}

		/// <summary>
		/// Updates the video source list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="sourceList"></param>
		public void SetVideoSourceList(List<SourceBaseListInfo> sourceList)
		{
			OnVideoSourceListUpdate.Raise(this, new VideoSourceBaseListEventArgs(sourceList));
		}

		private void SetVolumeLevelFeedback(float volume)
		{
			OnVolumeLevelFeedbackUpdate.Raise(this, new VolumeLevelFeedbackEventArgs(volume));
		}

		private void SetVolumeMuteFeedback(bool mute)
		{
			OnVolumeMuteFeedbackUpdate.Raise(this, new VolumeMuteFeedbackEventArgs(mute));
		}

		private void SetVolumeAvaliableControls(eVolumeLevelAvailableControl levelControl,
		                                         eVolumeMuteAvailableControl muteControl)
		{
			OnVolumeAvailableControlUpdate.Raise(this, new VolumeAvailableControlEventArgs(levelControl, muteControl));
		}

		#endregion

		#region VolumeDeviceControl

		public override float IncrementValue { get { return 0.03f; } }

		protected override void InstantiateVolumeControl(IVolumeDeviceControl volumeDevice)
		{
			// Test for ActiveVolumeControl being Null
			if (volumeDevice == null)
			{
				SetVolumeAvaliableControls(eVolumeLevelAvailableControl.None, eVolumeMuteAvailableControl.None);
				SetVolumeLevelFeedback(0);
				SetVolumeMuteFeedback(false);
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
			SetVolumeAvaliableControls(volumeAvailableControl, muteAvailableControl);
			SetVolumeLevelFeedback(volumeLevelFeedback);
			SetVolumeMuteFeedback(muteStateFeedback);
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
			SetVolumeLevelFeedback(args.VolumePosition);
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

		private void ControlOnMuteStateChanged(object sender, MuteDeviceMuteStateChangedApiEventArgs args)
		{
			SetVolumeMuteFeedback(args.Data);
		}

		#endregion

		#endregion

	}
}