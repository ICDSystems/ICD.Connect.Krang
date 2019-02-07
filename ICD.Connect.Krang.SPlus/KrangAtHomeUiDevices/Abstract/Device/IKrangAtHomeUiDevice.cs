using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device
{
	public interface IKrangAtHomeUiDevice
	{

		#region Events

		event EventHandler<IntEventArgs> OnSetRoomId;

		event EventHandler<IntEventArgs> OnSetAudioSourceId;

		event EventHandler<IntEventArgs> OnSetVideoSourceId;

		event EventHandler<FloatEventArgs> OnSetVolumeLevel;

		event EventHandler OnSetVolumeRampUp;

		event EventHandler OnSetVolumeRampDown;

		event EventHandler OnSetVolumeRampStop;

		event EventHandler<BoolEventArgs> OnSetVolumeMute;

		event EventHandler OnSetVolumeMuteToggle;

		#endregion



	}
}