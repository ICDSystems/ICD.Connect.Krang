using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
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

		private readonly ICore m_Core;

		/// <summary>
		/// Constructor.
		/// </summary>
		public OriginatorsChangeBroadcastHandler()
		{
			IBroadcaster broadcaster = new Broadcaster();
			broadcaster.SetBroadcastData(new OriginatorsChangeData());

			SetBroadcaster(broadcaster);

			m_Core = ServiceProvider.GetService<ICore>();
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

			OnRemoteOriginatorsChanged.Raise(this, new GenericEventArgs<HostSessionInfo>(data.HostSession));
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
