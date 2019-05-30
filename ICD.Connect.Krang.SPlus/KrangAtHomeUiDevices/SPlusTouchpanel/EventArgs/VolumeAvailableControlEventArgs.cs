using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Device;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	[Serializable]
	public sealed class VolumeAvailableControl
	{
		public eVolumeLevelAvailableControl LevelAvailableControl { get; set; }

		public eVolumeMuteAvailableControl MuteAvailableControl { get; set; }
	}

	public sealed class VolumeAvailableControlEventArgs : GenericEventArgs<VolumeAvailableControl>
	{

		public eVolumeLevelAvailableControl LevelAvailableControl { get { return Data.LevelAvailableControl; } }

		public eVolumeMuteAvailableControl MuteAvailableControl { get { return Data.MuteAvailableControl; } }

		public VolumeAvailableControlEventArgs(VolumeAvailableControl availableControl)
			: base(availableControl)
		{
			
		}

		public VolumeAvailableControlEventArgs(eVolumeLevelAvailableControl level, eVolumeMuteAvailableControl mute)
			: base(new VolumeAvailableControl()
			{
				LevelAvailableControl = level,
				MuteAvailableControl = mute
			})
		{
		}
	}
}