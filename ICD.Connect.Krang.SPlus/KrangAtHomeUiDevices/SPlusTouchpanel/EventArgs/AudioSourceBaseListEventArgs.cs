using System.Collections.Generic;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public sealed class AudioSourceBaseListEventArgs : AbstractSourceBaseListEventArgs
	{
		public AudioSourceBaseListEventArgs(List<SourceBaseListInfo> sourceList)
			: base(sourceList)
		{
		}
	}
}