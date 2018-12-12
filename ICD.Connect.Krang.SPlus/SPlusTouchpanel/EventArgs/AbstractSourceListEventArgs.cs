using System.Collections.Generic;
using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.EventArgs
{
	public abstract class AbstractSourceListEventArgs : AbstractGenericApiEventArgs<IEnumerable<KeyValuePair<ushort, SourceInfo>>>
	{
		protected AbstractSourceListEventArgs(string eventName, IEnumerable<KeyValuePair<ushort, SourceInfo>> sourceList)
			: base(eventName, sourceList)
		{
		}
	}
}