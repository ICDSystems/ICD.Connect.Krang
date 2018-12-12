using System;
using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.Device;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.Proxy;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.EventArgs
{
	[Serializable]
	public sealed class VolumeAvailableControl
	{
		public eVolumeLevelAvailableControl LevelAvailableControl { get; set; }

		public eVolumeMuteAvailableControl MuteAvailableControl { get; set; }
	}

	public sealed class VolumeAvailableControlEventArgs : AbstractGenericApiEventArgs<VolumeAvailableControl>
	{

		public eVolumeLevelAvailableControl LevelAvailableControl { get { return Data.LevelAvailableControl; } }

		public eVolumeMuteAvailableControl MuteAvailableControl { get { return Data.MuteAvailableControl; } }

		public VolumeAvailableControlEventArgs(VolumeAvailableControl availableControl)
			: base(SPlusTouchpanelDeviceApi.EVENT_VOLUME_AVAILABLE_CONTROL, availableControl)
		{
			
		}

		public VolumeAvailableControlEventArgs(eVolumeLevelAvailableControl level, eVolumeMuteAvailableControl mute)
			: base(SPlusTouchpanelDeviceApi.EVENT_VOLUME_AVAILABLE_CONTROL, new VolumeAvailableControl()
			{
				LevelAvailableControl = level,
				MuteAvailableControl = mute
			})
		{
		}
	}
}