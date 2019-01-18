using System;
using System.Collections.Generic;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Routing
{
	public sealed class SourceRoomsUsedUpdatedEventArgs : EventArgs
	{
		public ISource Source { get; private set; }

		public IEnumerable<IRoom> RoomsInUse { get; private set; }

		public SourceRoomsUsedUpdatedEventArgs(ISource source, IEnumerable<IRoom> roomsInUse)
		{
			Source = source;
			RoomsInUse = roomsInUse;
		}
	}
}