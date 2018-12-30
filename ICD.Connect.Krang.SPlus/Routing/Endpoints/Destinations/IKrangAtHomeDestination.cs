using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using ICD.Connect.Routing.Endpoints.Destinations;

namespace ICD.Connect.Krang.SPlus.Routing.Endpoints.Destinations
{
	[Flags]
	public enum eAudioOption
	{
		None,
		AudioOnly,
		AudioVideoOnly,
		All = AudioOnly | AudioVideoOnly
	}

	public interface IKrangAtHomeDestination : IDestination
	{
		eAudioOption AudioOption { get; }
	}
}