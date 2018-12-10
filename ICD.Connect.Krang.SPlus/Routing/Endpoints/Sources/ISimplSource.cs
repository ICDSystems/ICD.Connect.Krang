using System;
using ICD.Common.Properties;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Settings.Simpl;

namespace ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources
{
	public interface ISimplSource : ISource, ISimplOriginator
	{
		[PublicAPI]
		ushort CrosspointId { get; set; }

		[PublicAPI]
		ushort CrosspointType { get; set; }

		[PublicAPI]
		SimplSource.eSourceVisibility SourceVisibility { get; set; }

		/// <summary>
		/// Unique ID for the originator.
		/// </summary>
		int Id { get; set; }

		/// <summary>
		/// The name of the originator.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// The name that is used for the originator while in a combine space.
		/// </summary>
		string CombineName { get; set; }

		/// <summary>
		/// Raised when settings have been cleared.
		/// </summary>
		event EventHandler OnSettingsCleared;
	}
}