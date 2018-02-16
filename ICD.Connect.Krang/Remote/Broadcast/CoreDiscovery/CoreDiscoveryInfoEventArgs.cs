using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Krang.Remote.Broadcast.CoreDiscovery
{
	public sealed class CoreDiscoveryInfoEventArgs : GenericEventArgs<CoreDiscoveryInfo>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public CoreDiscoveryInfoEventArgs(CoreDiscoveryInfo data)
			: base(data)
		{
		}
	}
}
