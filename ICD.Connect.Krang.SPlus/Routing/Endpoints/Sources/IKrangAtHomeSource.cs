using System;
using ICD.Common.Properties;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Settings.Simpl;

namespace ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources
{
	public interface IKrangAtHomeSource : IKrangAtHomeSourceBase, ISource, ISimplOriginator
	{
		[PublicAPI]
		ushort CrosspointId { get; set; }

		[PublicAPI]
		ushort CrosspointType { get; set; }
	}
}