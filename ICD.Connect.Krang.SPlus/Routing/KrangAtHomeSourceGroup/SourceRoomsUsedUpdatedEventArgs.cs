using System;
using System.Collections.Generic;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup
{
	public sealed class SourceRoomsUsedUpdatedEventArgs: EventArgs
	{
		public IKrangAtHomeSource Source { get; private set; }

		public IEnumerable<IKrangAtHomeRoom> RoomsInUse { get; private set; }

		public SourceRoomsUsedUpdatedEventArgs(IKrangAtHomeSource source, IEnumerable<IKrangAtHomeRoom> roomsInUse)
		{
			Source = source;
			RoomsInUse = roomsInUse;
		}
	}
}