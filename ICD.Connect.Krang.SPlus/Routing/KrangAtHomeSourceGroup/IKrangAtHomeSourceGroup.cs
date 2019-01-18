using System.Collections.Generic;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup
{
	public interface IKrangAtHomeSourceGroup : IKrangAtHomeSourceBase
	{
		IEnumerable<IKrangAtHomeSource> GetSources();
	}
}