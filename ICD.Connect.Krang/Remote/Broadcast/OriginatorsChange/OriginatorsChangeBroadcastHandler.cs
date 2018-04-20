using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Broadcast.Broadcasters;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote.Broadcast.OriginatorsChange
{
	public sealed class OriginatorsChangeBroadcastHandler : AbstractBroadcastHandler<OriginatorsChangeData>
	{
		/// <summary>
		/// Raised when a remote broadcaster advertises an originator was added/removed.
		/// </summary>
		public event EventHandler<GenericEventArgs<HostInfo>> OnRemoteOriginatorsChanged;

		private readonly ICore m_Core;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="core"></param>
		public OriginatorsChangeBroadcastHandler(ICore core)
		{
			m_Core = core;

			IBroadcaster broadcaster = new Broadcaster();
			broadcaster.SetBroadcastData(new OriginatorsChangeData());

			SetBroadcaster(broadcaster);

			Subscribe(m_Core);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnRemoteOriginatorsChanged = null;

			base.Dispose();

			Unsubscribe(m_Core);
		}

		protected override void BroadcasterOnBroadcastReceived(object sender, BroadcastEventArgs eventArgs)
		{
			base.BroadcasterOnBroadcastReceived(sender, eventArgs);

			BroadcastData data = eventArgs.Data;

			OnRemoteOriginatorsChanged.Raise(this, new GenericEventArgs<HostInfo>(data.Source));
		}

		#region Core Callbacks

		private void Subscribe(ICore core)
		{
			core.Originators.OnChildrenChanged += OriginatorsOnOnChildrenChanged;
		}

		private void Unsubscribe(ICore core)
		{
			core.Originators.OnChildrenChanged -= OriginatorsOnOnChildrenChanged;
		}

		private void OriginatorsOnOnChildrenChanged(object sender, EventArgs eventArgs)
		{
			Broadcaster.Broadcast();
		}

		#endregion
	}
}
