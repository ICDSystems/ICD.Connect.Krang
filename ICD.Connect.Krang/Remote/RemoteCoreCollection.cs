using System.Collections;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Krang.Remote
{
	public sealed class RemoteCoreCollection : IEnumerable<RemoteCore>
	{
		private readonly Dictionary<HostInfo, RemoteCore> m_RemoteCores;
		private readonly SafeCriticalSection m_ProxySection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public RemoteCoreCollection()
		{
			m_RemoteCores = new Dictionary<HostInfo, RemoteCore>();
			m_ProxySection = new SafeCriticalSection();
		}

		/// <summary>
		/// Removes the remote core with the given host info from the collection.
		/// </summary>
		/// <param name="source"></param>
		public void Remove(HostInfo source)
		{
			m_ProxySection.Execute(() => m_RemoteCores.Remove(source));
		}

		/// <summary>
		/// Adds the remote core to the collection.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="remoteCore"></param>
		public void Add(HostInfo source, RemoteCore remoteCore)
		{
			m_ProxySection.Execute(() => m_RemoteCores.Add(source, remoteCore));
		}

		public IEnumerator<RemoteCore> GetEnumerator()
		{
			return m_ProxySection.Execute(() => m_RemoteCores.Values.ToList(m_RemoteCores.Count).GetEnumerator());
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool TryGetRemoteCore(HostInfo source, out RemoteCore remoteCore)
		{
			m_ProxySection.Enter();

			try
			{
				return m_RemoteCores.TryGetValue(source, out remoteCore);
			}
			finally
			{
				m_ProxySection.Leave();
			}
		}
	}
}
