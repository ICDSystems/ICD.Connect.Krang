using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Proxy;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.EventArgs
{
	public sealed class SetVideoSourceIdApiEventArgs : AbstractGenericApiEventArgs<int>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SetVideoSourceIdApiEventArgs(int data) : base(SPlusUiDeviceApi.EVENT_SET_VIDEO_SOURCE_ID, data)
		{
		}
	}
}