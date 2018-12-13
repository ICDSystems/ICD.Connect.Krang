using System.Collections;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Krang.Remote
{
	public sealed class RemoteCoreCollection : IEnumerable<RemoteCore>
	{
		private readonly Dictionary<HostInfo, RemoteCore> m_Proxies;
		private readonly SafeCriticalSection m_ProxySection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public RemoteCoreCollection()
		{
			m_Proxies = new Dictionary<HostInfo, RemoteCore>();
			m_ProxySection = new SafeCriticalSection();
		}

		/// <summary>
		/// Removes the proxy with the given host info from the collection.
		/// </summary>
		/// <param name="source"></param>
		public void Remove(HostInfo source)
		{
			m_ProxySection.Execute(() => m_Proxies.Remove(source));
		}

		/// <summary>
		/// Adds the proxy to the collection.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="proxy"></param>
		public void Add(HostInfo source, RemoteCore proxy)
		{
			m_ProxySection.Execute(() => m_Proxies.Add(source, proxy));
		}

		public IEnumerator<RemoteCore> GetEnumerator()
		{
			return m_ProxySection.Execute(() => m_Proxies.Values.ToList(m_Proxies.Count).GetEnumerator());
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool TryGetRemoteCore(HostInfo source, out RemoteCore proxy)
		{
			m_ProxySection.Enter();

			try
			{
				return m_Proxies.TryGetValue(source, out proxy);
			}
			finally
			{
				m_ProxySection.Leave();
			}
		}
	}
}
