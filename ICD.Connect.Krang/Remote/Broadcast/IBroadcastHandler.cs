using System;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Broadcast.Broadcasters;

namespace ICD.Connect.Krang.Remote.Broadcast
{
	public interface IBroadcastHandler : IDisposable, IConsoleNode
	{
		/// <summary>
		/// Gets the current broadcaster.
		/// </summary>
		IBroadcaster Broadcaster { get; }

		/// <summary>
		/// Sets the broadcaster for this handler.
		/// </summary>
		/// <param name="broadcaster"></param>
		void SetBroadcaster(IBroadcaster broadcaster);
	}
}