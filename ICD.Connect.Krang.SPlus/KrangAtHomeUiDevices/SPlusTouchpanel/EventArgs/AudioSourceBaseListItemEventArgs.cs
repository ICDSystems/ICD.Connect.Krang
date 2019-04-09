using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Proxy;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public class AudioSourceBaseListItemEventArgs : AbstractSourceBaseListItemEventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public AudioSourceBaseListItemEventArgs(SourceBaseListInfo data)
			: base(SPlusTouchpanelDeviceApi.EVENT_AUDIO_SOURCE_LIST_ITEM, data)
		{
		}
	}
}