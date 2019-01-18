using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup
{
	public class KrangAtHomeSourceGroup : AbstractOriginator<KrangAtHomeSourceGroupSettings>, IKrangAtHomeSourceBase
	{
		public IKrangAtHomeSource GetSource()
		{
			throw new System.NotImplementedException();
		}

		public KrangAtHomeSource.eSourceVisibility SourceVisibility { get; set; }
	}
}