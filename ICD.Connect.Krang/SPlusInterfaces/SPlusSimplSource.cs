#if SIMPLSHARP
using System;
using Crestron.SimplSharp;
using ICD.Common.Properties;
using ICD.Connect.Krang.Routing.Endpoints.Sources;


namespace ICD.Connect.Krang.SPlusInterfaces
{
	[PublicAPI("SPlus")]
	public sealed class SPlusSimplSource
	{
		private SimplSource m_Source;
		private ushort m_SourceId;

		public delegate void SPlusSourceInfoCallback(
			SimplSharpString sourceName, ushort sourceId, ushort sourceControlId, ushort sourceControlType);

		public SPlusSourceInfoCallback SPlusSourceInfo { get; set; }

		public SPlusSimplSource()
		{
			SPlusKrangBootstrap.OnKrangLoaded += SPlusKrangBootstrapOnKrangLoaded;
		}

		public void SetSourceId(ushort sourceId)
		{
			if (sourceId == m_SourceId)
				return;

			m_SourceId = sourceId;
			if (SPlusKrangBootstrap.Krang.RoutingGraph.Sources.ContainsChild(sourceId))
				m_Source = SPlusKrangBootstrap.Krang.RoutingGraph.Sources[sourceId] as SimplSource;
			else
				m_Source = null;

			UpdateSource();
		}

		private void UpdateSource()
		{
			SPlusSourceInfoCallback callback = SPlusSourceInfo;
			if (callback == null)
				return;

			if (m_Source == null)
				callback(new SimplSharpString(""), 0, 0, 0);
			else
				callback(new SimplSharpString(m_Source.Name), (ushort)m_Source.Id, m_Source.CrosspointId, m_Source.CrosspointType);
		}

		private void SPlusKrangBootstrapOnKrangLoaded(object sender, EventArgs eventArgs)
		{
			SetSourceId(m_SourceId);
		}
	}
}

#endif
