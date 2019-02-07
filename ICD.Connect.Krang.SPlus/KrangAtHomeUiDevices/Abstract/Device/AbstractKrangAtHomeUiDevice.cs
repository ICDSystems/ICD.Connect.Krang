using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices.Simpl;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device
{
	public abstract class AbstractKrangAtHomeUiDevice<TSettings> : AbstractSimplDevice<TSettings>, IKrangAtHomeUiDevice
		where TSettings : ISimplDeviceSettings, new()
	{

		#region Events to UI

		public event EventHandler<IntEventArgs> OnSetRoomId;
		public event EventHandler<IntEventArgs> OnSetAudioSourceId;
		public event EventHandler<IntEventArgs> OnSetVideoSourceId;
		public event EventHandler<FloatEventArgs> OnSetVolumeLevel;
		public event EventHandler OnSetVolumeRampUp;
		public event EventHandler OnSetVolumeRampDown;
		public event EventHandler OnSetVolumeRampStop;
		public event EventHandler<BoolEventArgs> OnSetVolumeMute;
		public event EventHandler OnSetVolumeMuteToggle;

		#endregion


		#region Methods From Shim

		public void SetRoomId(int id)
		{
			OnSetRoomId.Raise(this, new IntEventArgs(id));
		}

		public void SetAudioSourceId(int id)
		{
			OnSetAudioSourceId.Raise(this, new IntEventArgs(id));
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


	}
}