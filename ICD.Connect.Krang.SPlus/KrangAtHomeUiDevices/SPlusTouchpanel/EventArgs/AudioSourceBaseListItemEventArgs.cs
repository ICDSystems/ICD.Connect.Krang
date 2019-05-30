using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public sealed class AudioSourceBaseListItemEventArgs : AbstractSourceBaseListItemEventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public AudioSourceBaseListItemEventArgs(SourceBaseListInfo data)
			: base(data)
		{
		}
	}
}