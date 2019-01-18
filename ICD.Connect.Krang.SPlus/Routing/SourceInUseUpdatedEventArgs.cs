using System;
using ICD.Connect.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Routing
{
	public sealed class SourceInUseUpdatedEventArgs : EventArgs
	{
		public ISource Source { get; private set; }

		public bool SourceInUse { get; private set; }

		public SourceInUseUpdatedEventArgs(ISource source, bool sourceInUse)
		{
			Source = source;
			SourceInUse = sourceInUse;
		}

	}
}