using System.Collections.Generic;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Proxy;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public sealed class VideoSourceBaseListEventArgs : AbstractSourceBaseListEventArgs
	{
		public VideoSourceBaseListEventArgs(List<SourceBaseListInfo> sourceList)
			: base(SPlusTouchpanelDeviceApi.EVENT_VIDEO_SOURCE_LIST, sourceList)
		{
		}
	}
}