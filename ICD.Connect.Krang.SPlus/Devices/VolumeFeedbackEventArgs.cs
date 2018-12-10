using System;

namespace ICD.Connect.Krang.SPlus.Devices
{
	public class VolumeFeedbackEventArgs : EventArgs
	{

		public float Volume { get; private set; }

		public bool Mute { get; private set; }

		public VolumeFeedbackEventArgs(float volume, bool mute)
		{
			Volume = volume;
			Mute = mute;
		}
	}
}