using System.Collections.Generic;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.Proxy;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.EventArgs
{
	public class VideoSourceListEventArgs : AbstractSourceListEventArgs
	{
		public VideoSourceListEventArgs(IEnumerable<KeyValuePair<ushort, SourceInfo>> sourceList)
			: base(SPlusTouchpanelDeviceApi.EVENT_VIDEO_SOURCE_LIST, sourceList)
		{
		}
	}
}