using System.Collections.Generic;
using System.Linq;
using ICD.Connect.API.Nodes;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.Core
{
	public sealed class ApiOriginatorsNodeGroup<TOriginator> : AbstractApiNodeGroup
		where TOriginator : class, IOriginator
	{
		private readonly IOriginatorCollection<IOriginator> m_Originators;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="originators"></param>
		public ApiOriginatorsNodeGroup(IOriginatorCollection<IOriginator> originators)
		{
			m_Originators = originators;
		}

		public override object this[uint key] { get { return m_Originators.GetChild<TOriginator>((int)key); } }

		public override bool ContainsKey(uint key)
		{
			return m_Originators.ContainsChild<TOriginator>((int)key);
		}

		public override IEnumerable<KeyValuePair<uint, object>> GetKeyedNodes()
		{
			return m_Originators.GetChildren<TOriginator>().Select(o => new KeyValuePair<uint, object>((uint)o.Id, o));
		}
	}
}
