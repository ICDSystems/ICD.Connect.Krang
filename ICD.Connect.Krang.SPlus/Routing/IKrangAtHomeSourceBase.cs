using ICD.Common.Properties;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Settings.Originators;

namespace ICD.Connect.Krang.SPlus.Routing
{
	public interface IKrangAtHomeSourceBase : IOriginator
	{
		IKrangAtHomeSource GetSource();

		[PublicAPI]
		KrangAtHomeSource.eSourceVisibility SourceVisibility { get; set; }
	}
}