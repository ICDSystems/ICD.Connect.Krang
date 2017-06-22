using System;
using System.Collections.Generic;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Endpoints.Groups;
using ICD.Connect.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.Remote.Direct
{
	[Serializable]
	public sealed class ShareDevicesMessage : AbstractMessage
	{
		public IEnumerable<ISource> Sources { get; set; }
		public IEnumerable<IDestination> Destinations { get; set; }
		public IEnumerable<IDestinationGroup> DestinationGroups { get; set; }
		public Dictionary<int, IEnumerable<ConnectorInfo>> SourceConnections { get; set; }
		public Dictionary<int, IEnumerable<ConnectorInfo>> DestinationConnections { get; set; }
	}
}
