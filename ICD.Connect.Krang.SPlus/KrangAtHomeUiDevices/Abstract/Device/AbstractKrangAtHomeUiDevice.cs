using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls.Mute;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Extensions;
using ICD.Connect.Devices.Simpl;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.EventArgs;
using ICD.Connect.Settings.Cores;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device
{
	public abstract class AbstractKrangAtHomeUiDevice<TSettings> : AbstractSimplDevice<TSettings>, IKrangAtHomeUiDevice
		where TSettings : ISimplDeviceSettings, new()
	{

		#region Events to UI

		public event EventHandler<SetRoomIdApiEventArgs> OnSetRoomId;
		public event EventHandler<SetAudioSourceIdApiEventArgs> OnSetAudioSourceId;
		public event EventHandler<SetVideoSourceIdApiEventArgs> OnSetVideoSourceId;
		
		public void SetVolumeControl(DeviceControlInfo controlInfo)
		{
			if (controlInfo.DeviceId == 0)
			{
				SetVolumeControl(null);
				return;
			}

			IVolumeDeviceControl control = null;

			try
			{
				control = ServiceProvider.GetService<ICore>().GetControl<IVolumeDeviceControl>(controlInfo);
			}
			catch (KeyNotFoundException)
			{
				Log(eSeverity.Error, "Volume control not found at {0}", VolumeControl);
			}
			catch (InvalidCastException)
			{
				Log(eSeverity.Error, "Control at {0} is not a volume control", VolumeControl);
			}
			SetVolumeControl(control);
		}

		#endregion

		public IVolumeDeviceControl VolumeControl { get; set; }
		public IVolumePositionDeviceControl VolumePositionControl { get; private set; }

		public IVolumeMuteFeedbackDeviceControl VolumeMuteFeedbackControl { get; private set; }

		public abstract float IncrementValue { get; }

		#region Methods From Shim

		public void SetRoomId(int id)
		{
			OnSetRoomId.Raise(this, new SetRoomIdApiEventArgs(id));
		}

		public void SetAudioSourceId(int id)
		{
			OnSetAudioSourceId.Raise(this, new SetAudioSourceIdApiEventArgs(id));
		}

		public void SetVideoSourcdId(int id)
		{
			OnSetVideoSourceId.Raise(this, new SetVideoSourceIdApiEventArgs(id));
		}

		public void SetVolumeLevel(float volumeLevel)
		{
			IVolumePositionDeviceControl control = VolumeControl as IVolumePositionDeviceControl;

			if (control != null)
				control.SetVolumePosition(volumeLevel);
		}

		public void SetVolumeRampUp()
		{
			// todo: fix ramping RPC issues and re-enable ramping

			IVolumeLevelDeviceControl control = VolumeControl as IVolumeLevelDeviceControl;
			if (control != null)
				control.VolumeLevelIncrement(IncrementValue);

			/*
			IVolumeLevelDeviceControl controlLvl = VolumeControl as IVolumeLevelDeviceControl;
			IVolumeRampDeviceControl control = VolumeControl as IVolumeRampDeviceControl;

			if (controlLvl != null)
				controlLvl.VolumePositionRampUp(0.03f);
			else if (control != null)
				control.VolumeRampUp();
			*/
		}

		public void SetVolumeRampDown()
		{
			// todo: fix ramping RPC issues and re-enable ramping


			IVolumeLevelDeviceControl control = VolumeControl as IVolumeLevelDeviceControl;
			if (control != null)
				control.VolumeLevelDecrement(IncrementValue);

			/*
			IVolumeLevelDeviceControl controlLvl = VolumeControl as IVolumeLevelDeviceControl;
			IVolumeRampDeviceControl control = VolumeControl as IVolumeRampDeviceControl;

			if (controlLvl != null)
				controlLvl.VolumePositionRampDown(0.03f);
			else if (control != null)
				control.VolumeRampDown();
			*/
		}

		public void SetVolumeRampStop()
		{
			// todo: fix ramping RPC issues and re-enable ramping

			/*
			IVolumeRampDeviceControl control = VolumeControl as IVolumeRampDeviceControl;

			if (control != null)
				control.VolumeRampStop();
			*/
		}

		public void SetVolumeMute(bool state)
		{
			IVolumeMuteDeviceControl control = VolumeControl as IVolumeMuteDeviceControl;

			if (control != null)
				control.SetVolumeMute(state);
		}

		public void SetVolumeMuteToggle()
		{
			IVolumeMuteBasicDeviceControl control = VolumeControl as IVolumeMuteBasicDeviceControl;

			if (control != null)
				control.VolumeMuteToggle();
		}


		#endregion

		#region Volume

		private void SetVolumeControl(IVolumeDeviceControl control)
		{
			if (VolumeControl == control)
				return;

			Unsubscribe(VolumeControl);

			VolumeControl = control;

			Subscribe(VolumeControl);

			InstantiateVolumeControl(VolumeControl);
		}

		private void Subscribe(IVolumeDeviceControl volumeDevice)
		{
			if (volumeDevice == null)
				return;

			VolumePositionControl = volumeDevice as IVolumePositionDeviceControl;
			SubscribeVolumePositionDeviceControl(VolumePositionControl);

			VolumeMuteFeedbackControl = volumeDevice as IVolumeMuteFeedbackDeviceControl;
			SubscribeVolumeMuteFeedbackDeviceControl(VolumeMuteFeedbackControl);

		}

		private void Unsubscribe(IVolumeDeviceControl volumeDevice)
		{
			if (volumeDevice == null)
				return;

			UnsubscribeVolumePositionDeviceControl(VolumePositionControl);
			UnsubscribeVolumeMuteFeedbackDeviceControl(VolumeMuteFeedbackControl);
		}

		protected abstract void InstantiateVolumeControl(IVolumeDeviceControl volumeDevice);

		#region VolumePositionDeviceControl

		protected virtual void SubscribeVolumePositionDeviceControl(IVolumePositionDeviceControl control)
		{
		}

		protected virtual void UnsubscribeVolumePositionDeviceControl(IVolumePositionDeviceControl control)
		{
		}

		#endregion

		#region MuteFeedbackDeviceControl

		protected virtual void SubscribeVolumeMuteFeedbackDeviceControl(IVolumeMuteFeedbackDeviceControl control)
		{
		}

		protected virtual void UnsubscribeVolumeMuteFeedbackDeviceControl(IVolumeMuteFeedbackDeviceControl control)
		{
		}

		#endregion

		#endregion


	}
}