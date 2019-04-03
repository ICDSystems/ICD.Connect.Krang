using System;
using ICD.Common.Properties;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Routing
{
	[Flags]
	public enum eSourceVisibility
	{
		None = 0,
		Audio = 1,
		Video = 2,
	}

	public interface IKrangAtHomeSourceBase : ISourceBase
	{
		[PublicAPI]
		eSourceVisibility SourceVisibility { get; set; }

		[PublicAPI]
		eKrangAtHomeSourceIcon? SourceIcon { get; set; }

		int Order { get; }
	}
}