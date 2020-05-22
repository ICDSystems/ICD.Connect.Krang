using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Broadcast.Broadcasters;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Cores;

namespace ICD.Connect.Krang.Remote.Broadcast.OriginatorsChange
{
	public sealed class OriginatorsChangeBroadcastHandler : AbstractBroadcastHandler<OriginatorsChangeData>
	{
		/// <summary>
		/// Raised when a remote broadcaster advertises an originator was added/removed.
		/// </summary>
		public event EventHandler<GenericEventArgs<HostSessionInfo>> OnRemoteOriginatorsChanged;

		/// <summary>
		/// Constructor.
		/// </summary>
		public OriginatorsChangeBroadcastHandler()
		{
			IBroadcaster broadcaster = new Broadcaster();
			broadcaster.SetBroadcastData(new OriginatorsChangeData());

			SetBroadcaster(broadcaster);

			Subscribe(Core);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnRemoteOriginatorsChanged = null;

			base.Dispose();

			Unsubscribe(Core);
		}

		protected override void BroadcasterOnBroadcastReceived(object sender, BroadcastEventArgs eventArgs)
		{
			base.BroadcasterOnBroadcastReceived(sender, eventArgs);

			BroadcastData data = eventArgs.Data;

			OnRemoteOriginatorsChanged.Raise(this, new GenericEventArgs<HostSessionInfo>(data.HostSession));
		}

		#region Core Callbacks

		private void Subscribe(ICore core)
		{
			core.Originators.OnChildrenChanged += OriginatorsOnChildrenChanged;
		}

		private void Unsubscribe(ICore core)
		{
			core.Originators.OnChildrenChanged -= OriginatorsOnChildrenChanged;
		}

		private void OriginatorsOnChildrenChanged(object sender, EventArgs eventArgs)
		{
			Broadcaster.Broadcast();
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public override string ConsoleName { get { return "OriginatorsChanged"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp { get
		{
			return "Handles messages from remote cores that their originators changed";
		} }

		#endregion
	}
}
