using System.Collections.Generic;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.Proxy;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.EventArgs
{
	public sealed class VideoSourceListEventArgs : AbstractSourceListEventArgs
	{
		public VideoSourceListEventArgs(List<SourceInfo> sourceList)
			: base(SPlusTouchpanelDeviceApi.EVENT_VIDEO_SOURCE_LIST, sourceList)
		{
		}
	}
}