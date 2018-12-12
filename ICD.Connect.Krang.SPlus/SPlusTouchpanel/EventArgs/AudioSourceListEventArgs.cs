using System.Collections.Generic;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.Proxy;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.EventArgs
{
	public class AudioSourceListEventArgs : AbstractSourceListEventArgs
	{
		public AudioSourceListEventArgs(IEnumerable<KeyValuePair<ushort, SourceInfo>> sourceList)
			: base(SPlusTouchpanelDeviceApi.EVENT_AUDIO_SOURCE_LIST, sourceList)
		{
		}
	}
}