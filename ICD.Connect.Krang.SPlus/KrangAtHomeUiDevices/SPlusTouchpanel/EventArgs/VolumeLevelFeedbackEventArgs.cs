using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public sealed class VolumeLevelFeedbackEventArgs : GenericEventArgs<float>
	{
		public VolumeLevelFeedbackEventArgs(float level) : base(level)
		{
			
		}
	}
}