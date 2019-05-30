using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Proxy;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.EventArgs
{
	public sealed class SetAudioSourceIdApiEventArgs : AbstractGenericApiEventArgs<int>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SetAudioSourceIdApiEventArgs(int data) : base(SPlusUiDeviceApi.EVENT_SET_AUDIO_SOURCE_ID, data)
		{
		}
	}
}