using System;
using ICD.Connect.Routing.Endpoints.Destinations;

namespace ICD.Connect.Krang.SPlus.Routing.Endpoints.Destinations
{
	[Flags]
	public enum eAudioOption
	{
		None = 0,
		AudioOnly = 1,
		AudioVideoOnly = 2,
		All = AudioOnly | AudioVideoOnly
	}

	public interface IKrangAtHomeDestination : IDestination
	{
		eAudioOption AudioOption { get; }
	}
}