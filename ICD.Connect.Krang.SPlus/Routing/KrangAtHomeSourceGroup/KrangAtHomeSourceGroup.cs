using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Settings.Originators;

namespace ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup
{
	public sealed class KrangAtHomeSourceGroup : AbstractOriginator<KrangAtHomeSourceGroupSettings>, IKrangAtHomeSourceBase
	{
		public IKrangAtHomeSource GetSource()
		{
			throw new System.NotImplementedException();
		}

		public KrangAtHomeSource.eSourceVisibility SourceVisibility { get; set; }
	}
}