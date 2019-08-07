using ICD.Connect.API.EventArguments;
using ICD.Connect.API.Info;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Proxy;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public sealed class RequestRefreshApiEventArgs : AbstractApiEventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public RequestRefreshApiEventArgs() : base(SPlusUiDeviceApi.EVENT_REQUEST_REFRESH)
		{
		}

		/// <summary>
		/// Builds an API result for the args.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public override void BuildResult(object sender, ApiResult result)
		{
		}
	}
}