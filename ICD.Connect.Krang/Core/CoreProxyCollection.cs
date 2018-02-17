using System.Collections;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Krang.Core
{
	public sealed class CoreProxyCollection : IEnumerable<CoreProxy>
	{
		private readonly Dictionary<int, CoreProxy> m_Proxies;
		private readonly SafeCriticalSection m_ProxySection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public CoreProxyCollection()
		{
			m_Proxies = new Dictionary<int, CoreProxy>();
			m_ProxySection = new SafeCriticalSection();
		}

		/// <summary>
		/// Removes the proxy with the given id from the collection.
		/// </summary>
		/// <param name="id"></param>
		public void Remove(int id)
		{
			m_ProxySection.Execute(() => m_Proxies.Remove(id));
		}

		/// <summary>
		/// Adds the proxy to the collection.
		/// </summary>
		/// <param name="proxy"></param>
		public void Add(CoreProxy proxy)
		{
			m_ProxySection.Execute(() => m_Proxies.Add(proxy.Id, proxy));
		}

		public IEnumerator<CoreProxy> GetEnumerator()
		{
			return m_ProxySection.Execute(() => m_Proxies.Values.ToList(m_Proxies.Count).GetEnumerator());
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool TryGetProxy(int id, out CoreProxy proxy)
		{
			m_ProxySection.Enter();

			try
			{
				return m_Proxies.TryGetValue(id, out proxy);
			}
			finally
			{
				m_ProxySection.Leave();
			}
		}
	}
}
