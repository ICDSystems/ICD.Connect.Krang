using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Proxy;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public sealed class SetVideoSourceIndexApiEventArgs : AbstractGenericApiEventArgs<int>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SetVideoSourceIndexApiEventArgs(int data)
			: base(SPlusTouchpanelDeviceApi.EVENT_SET_VIDEO_SOURCE_INDEX, data)
		{
		}
	}
}