using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Proxy;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.EventArgs
{
	public sealed class SourceChangedApiEventArgs : AbstractGenericApiEventArgs<SourceInfo>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SourceChangedApiEventArgs(SourceInfo data) : base(SPlusRemoteApi.EVENT_SOURCE_CHANGED, data)
		{
		}

		public SourceChangedApiEventArgs(IKrangAtHomeSource source)
			: base(SPlusRemoteApi.EVENT_SOURCE_CHANGED, new SourceInfo(source))
		{
			
		}
	}
}