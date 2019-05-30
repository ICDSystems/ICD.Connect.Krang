using ICD.Common.Utils.EventArguments;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public abstract class AbstractSourceBaseListItemEventArgs : GenericEventArgs<SourceBaseListInfo>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		protected AbstractSourceBaseListItemEventArgs(SourceBaseListInfo data) : base(data)
		{
		}
	}
}