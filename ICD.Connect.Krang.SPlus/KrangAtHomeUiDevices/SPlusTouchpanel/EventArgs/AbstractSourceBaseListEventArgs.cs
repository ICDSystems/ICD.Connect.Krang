using System.Collections.Generic;
using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public abstract class AbstractSourceBaseListEventArgs : AbstractGenericApiEventArgs<List<SourceBaseInfo>>
	{
		protected AbstractSourceBaseListEventArgs(string eventName, List<SourceBaseInfo> sourceList)
			: base(eventName, sourceList)
		{
		}
	}
}