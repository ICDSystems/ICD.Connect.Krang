using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Controls.Mute;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.States
{
	public sealed class RoomState
	{
		public event EventHandler<BoolEventArgs> OnMuteStateChanged;
		public event EventHandler<FloatEventArgs> OnVolumePositionChanged;
		public event EventHandler<BoolEventArgs> OnVolumeAvaliabilityChanged;
		public event EventHandler OnActiveSourceChanged;

		private readonly IKrangAtHomeRoom m_Room;

		private IVolumeDeviceControl m_VolumeControl;
		private float m_VolumePostition;
		private bool m_Muted;
		private bool m_VolumeAvaliable;

		public RoomState(IKrangAtHomeRoom room)
		{
			m_Room = room;
			Subscribe(m_Room);

			SetVolumeControl(m_Room.ActiveVolumeControl);
		}

		public void VolumeUp()
		{
			IVolumeLevelDeviceControl control = m_VolumeControl as IVolumeLevelDeviceControl;
			if (control != null)
				//control.VolumePositionRampUp(.03f);
				control.VolumeLevelIncrement(4);
		}

		public void VolumeDown()
		{
			IVolumeLevelDeviceControl control = m_VolumeControl as IVolumeLevelDeviceControl;
			if (control != null)
				//control.VolumePositionRampDown(.03f);
				control.VolumeLevelDecrement(4);
		}

		public void VolumeStop()
		{
			/*
			IVolumeLevelDeviceControl control = m_VolumeControl as IVolumeLevelDeviceControl;
			if (control != null)
				control.VolumeRampStop();
			*/
		}

		public void ToggleMute()
		{
			IVolumeMuteDeviceControl control = m_VolumeControl as IVolumeMuteDeviceControl;
			if (control != null)
				control.VolumeMuteToggle();
		}

		private void SetVolumeControl(IVolumeDeviceControl activeVolumeControl)
		{
			if (activeVolumeControl == m_VolumeControl)
				return;

			Unsubscribe(m_VolumeControl);
			m_VolumeControl = activeVolumeControl;
			Subscribe(m_VolumeControl);

			IVolumePositionDeviceControl positionControl = m_VolumeControl as IVolumePositionDeviceControl;
			IVolumeMuteFeedbackDeviceControl muteFeedbackControl = m_VolumeControl as IVolumeMuteFeedbackDeviceControl;

			VolumePostition = positionControl == null ? 0.0f : positionControl.VolumePosition;
			Muted = muteFeedbackControl != null && muteFeedbackControl.VolumeIsMuted;
			VolumeAvaliable = m_VolumeControl != null;
		}

		public float VolumePostition
		{
			get
			{
				return m_VolumePostition;
			}
			set
			{
				if (value == m_VolumePostition)
					return;

				m_VolumePostition = value;

				OnVolumePositionChanged.Raise(this, new FloatEventArgs(m_VolumePostition));
			}
		}

		public bool Muted
		{
			get
			{
				return m_Muted;
			}
			set
			{
				if (value == m_Muted)
					return;

				m_Muted = value;

				OnMuteStateChanged.Raise(this, new BoolEventArgs(m_Muted));
			}
		}

		public bool VolumeAvaliable
		{
			get { return m_VolumeAvaliable; }
			private set
			{
				if (value == m_VolumeAvaliable)
					return;

				m_VolumeAvaliable = value;

				OnVolumeAvaliabilityChanged.Raise(this, new BoolEventArgs(m_VolumeAvaliable));
			}
		}

		public string Source
		{
			get
			{
				IKrangAtHomeSource source = m_Room.GetSource();
				return source != null ? source.Name : null;
			}
		}

		public IKrangAtHomeRoom Room { get { return m_Room; } }

		private void Unsubscribe(IVolumeDeviceControl volumeControl)
		{
			IVolumePositionDeviceControl positionControl = volumeControl as IVolumePositionDeviceControl;
			IVolumeMuteFeedbackDeviceControl muteFeedbackControl = volumeControl as IVolumeMuteFeedbackDeviceControl;

			if (positionControl != null)
				positionControl.OnVolumeChanged -= PositionControlOnVolumeChanged;

			if (muteFeedbackControl != null)
				muteFeedbackControl.OnMuteStateChanged -= MuteFeedbackControlOnMuteStateChanged;
		}

		private void Subscribe(IVolumeDeviceControl volumeControl)
		{
			IVolumePositionDeviceControl positionControl = volumeControl as IVolumePositionDeviceControl;
			IVolumeMuteFeedbackDeviceControl muteFeedbackControl = volumeControl as IVolumeMuteFeedbackDeviceControl;

			if (positionControl != null)
				positionControl.OnVolumeChanged += PositionControlOnVolumeChanged;

			if (muteFeedbackControl != null)
				muteFeedbackControl.OnMuteStateChanged += MuteFeedbackControlOnMuteStateChanged;
		}

		private void MuteFeedbackControlOnMuteStateChanged(object sender, MuteDeviceMuteStateChangedApiEventArgs boolEventArgs)
		{
			Muted = boolEventArgs.Data;
		}

		private void PositionControlOnVolumeChanged(object sender, VolumeDeviceVolumeChangedEventArgs volumeDeviceVolumeChangedEventArgs)
		{
			VolumePostition = volumeDeviceVolumeChangedEventArgs.VolumePosition;
		}

		private void Subscribe(IKrangAtHomeRoom room)
		{
			room.OnActiveVolumeControlChanged += RoomOnActiveVolumeControlChanged;
			room.OnActiveSourcesChange += RoomOnActiveSourcesChange;
		}

		private void RoomOnActiveSourcesChange(object sender, EventArgs eventArgs)
		{
			OnActiveSourceChanged.Raise(this);
		}

		private void RoomOnActiveVolumeControlChanged(object sender, GenericEventArgs<IVolumeDeviceControl> genericEventArgs)
		{
			SetVolumeControl(genericEventArgs.Data);
		}
	}
}
