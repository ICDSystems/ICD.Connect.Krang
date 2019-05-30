using ICD.Common.Utils.EventArguments;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.EventArgs
{
	public sealed class SourceChangedEventArgs : GenericEventArgs<SourceInfo>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SourceChangedEventArgs(SourceInfo data) : base(data)
		{
		}

		public SourceChangedEventArgs(IKrangAtHomeSource source)
			: base(new SourceInfo(source))
		{
			
		}
	}
}