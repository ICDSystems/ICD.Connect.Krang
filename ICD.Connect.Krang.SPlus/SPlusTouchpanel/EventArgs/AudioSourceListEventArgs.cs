using System.Collections.Generic;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.Proxy;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.EventArgs
{
	public sealed class AudioSourceListEventArgs : AbstractSourceListEventArgs
	{
		public AudioSourceListEventArgs(List<SourceInfo> sourceList)
			: base(SPlusTouchpanelDeviceApi.EVENT_AUDIO_SOURCE_LIST, sourceList)
		{
		}
	}
}