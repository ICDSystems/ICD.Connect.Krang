using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.Proxy;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.EventArgs
{
	public sealed class VolumeLevelFeedbackEventArgs : AbstractGenericApiEventArgs<float>
	{
		public VolumeLevelFeedbackEventArgs(float level) : base(SPlusTouchpanelDeviceApi.EVENT_VOLUME_LEVEL_FEEDBACK, level)
		{
			
		}
	}
}