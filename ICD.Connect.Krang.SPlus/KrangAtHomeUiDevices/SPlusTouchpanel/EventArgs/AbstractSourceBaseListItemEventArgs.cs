using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public abstract class AbstractSourceBaseListItemEventArgs : AbstractGenericApiEventArgs<SourceBaseListInfo>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="eventName"></param>
		/// <param name="data"></param>
		public AbstractSourceBaseListItemEventArgs(string eventName, SourceBaseListInfo data) : base(eventName, data)
		{
		}
	}
}