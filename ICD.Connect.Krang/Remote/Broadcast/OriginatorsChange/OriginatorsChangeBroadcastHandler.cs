using System;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Broadcast.Broadcasters;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote.Broadcast.OriginatorsChange
{
	public sealed class OriginatorsChangeBroadcastHandler : AbstractBroadcastHandler<OriginatorsChangeData>
	{
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
			base.Dispose();

			Unsubscribe(m_Core);
		}

		protected override void BroadcasterOnBroadcastReceived(object sender, BroadcastEventArgs eventArgs)
		{
			base.BroadcasterOnBroadcastReceived(sender, eventArgs);
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
