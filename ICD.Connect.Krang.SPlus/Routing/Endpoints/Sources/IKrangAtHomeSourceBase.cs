using ICD.Common.Properties;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources
{
	public interface IKrangAtHomeSourceBase : IOriginator
	{
		IKrangAtHomeSource GetSource();

		[PublicAPI]
		KrangAtHomeSource.eSourceVisibility SourceVisibility { get; set; }
	}
}