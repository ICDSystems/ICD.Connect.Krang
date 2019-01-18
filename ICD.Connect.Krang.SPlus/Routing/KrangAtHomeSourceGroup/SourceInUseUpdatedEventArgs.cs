using System;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup
{
	public sealed class SourceInUseUpdatedEventArgs : EventArgs
	{
		public IKrangAtHomeSource Source { get; private set; }

		public bool SourceInUse { get; private set; }

		public SourceInUseUpdatedEventArgs(IKrangAtHomeSource source, bool sourceInUse)
		{
			Source = source;
			SourceInUse = sourceInUse;
		}

	}
}