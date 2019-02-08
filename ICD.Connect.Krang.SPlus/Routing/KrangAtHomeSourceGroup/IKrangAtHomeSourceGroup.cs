using System.Collections.Generic;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup
{
	public interface IKrangAtHomeSourceGroup : IKrangAtHomeSourceBase
	{
		/// <summary>
		/// The number of sources in the group
		/// </summary>
		int Count { get; }

		IEnumerable<IKrangAtHomeSource> GetSources();
	}
}