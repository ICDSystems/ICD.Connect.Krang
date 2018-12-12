using System.Collections.Generic;
using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.EventArgs
{
	public abstract class AbstractSourceListEventArgs : AbstractGenericApiEventArgs<List<SourceInfo>>
	{
		protected AbstractSourceListEventArgs(string eventName, List<SourceInfo> sourceList)
			: base(eventName, sourceList)
		{
		}
	}
}