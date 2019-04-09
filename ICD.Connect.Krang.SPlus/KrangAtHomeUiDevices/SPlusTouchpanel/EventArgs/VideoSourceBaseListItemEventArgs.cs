using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Proxy;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public sealed class VideoSourceBaseListItemEventArgs : AbstractSourceBaseListItemEventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public VideoSourceBaseListItemEventArgs(SourceBaseListInfo data) : base(SPlusTouchpanelDeviceApi.EVENT_VIDEO_SOURCE_LIST_ITEM, data)
		{
		}
	}
}