using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.Proxy;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.EventArgs
{
	public class VolumeMuteFeedbackEventArgs : AbstractGenericApiEventArgs<bool>
	{
		public VolumeMuteFeedbackEventArgs(bool state) : base(SPlusTouchpanelDeviceApi.EVENT_VOLUME_MUTE_FEEDBACK, state)
		{

		}
	}
}