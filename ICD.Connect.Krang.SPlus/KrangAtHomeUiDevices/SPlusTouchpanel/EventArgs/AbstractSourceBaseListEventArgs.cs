using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public abstract class AbstractSourceBaseListEventArgs : GenericEventArgs<List<SourceBaseListInfo>>
	{
		protected AbstractSourceBaseListEventArgs(List<SourceBaseListInfo> sourceList)
			: base(sourceList)
		{
		}
	}
}