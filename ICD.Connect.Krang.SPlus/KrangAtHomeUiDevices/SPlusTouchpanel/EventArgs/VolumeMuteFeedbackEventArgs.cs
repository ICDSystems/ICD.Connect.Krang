using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public sealed class VolumeMuteFeedbackEventArgs : GenericEventArgs<bool>
	{
		public VolumeMuteFeedbackEventArgs(bool state) : base(state)
		{

		}
	}
}