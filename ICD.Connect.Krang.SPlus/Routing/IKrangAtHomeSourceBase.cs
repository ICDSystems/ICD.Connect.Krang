﻿using System;
using ICD.Common.Properties;
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

		int Order { get; }
	}
}