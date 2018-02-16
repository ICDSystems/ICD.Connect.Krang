using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote.Broadcast.CoreDiscovery
{
	/// <summary>
	/// CoreDiscoveryBroadcastHandler simply tracks the discovered core instances on the network.
	/// </summary>
	public sealed class CoreDiscoveryBroadcastHandler : AbstractBroadcastHandler<CoreDiscoveryData>
	{
		private const long DEFAULT_INTERVAL = 30 * 1000;
		private const long TIMEOUT_INTERVAL = DEFAULT_INTERVAL / 5;
		private const long TIMEOUT_DURATION = DEFAULT_INTERVAL * 5;

		/// <summary>
		/// Raised when we discover a new core instance on the network.
		/// </summary>
		public event EventHandler<CoreDiscoveryInfoEventArgs> OnCoreDiscovered;

		/// <summary>
		/// Raised when a discovered core instance times out.
		/// </summary>
		public event EventHandler<CoreDiscoveryInfoEventArgs> OnCoreLost;

		private readonly Dictionary<int, CoreDiscoveryInfo> m_Discovered;
		private readonly SafeCriticalSection m_DiscoveredSection;
		private readonly SafeTimer m_TimeoutTimer;

		public ICore Core { get { return ServiceProvider.GetService<ICore>(); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public CoreDiscoveryBroadcastHandler()
		{
			m_Discovered = new Dictionary<int, CoreDiscoveryInfo>();
			m_DiscoveredSection = new SafeCriticalSection();
			m_TimeoutTimer = SafeTimer.Stopped(TimeoutCallback);

			SetBroadcaster(new RecurringBroadcaster<CoreDiscoveryData>(DEFAULT_INTERVAL));

			m_TimeoutTimer.Reset(TIMEOUT_INTERVAL, TIMEOUT_INTERVAL);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnCoreDiscovered = null;
			OnCoreLost = null;

			base.Dispose();

			m_TimeoutTimer.Stop();
		}

		/// <summary>
		/// Called periodically to cull any cores that have timed out.
		/// </summary>
		private void TimeoutCallback()
		{
			IcdHashSet<CoreDiscoveryInfo> dropped = new IcdHashSet<CoreDiscoveryInfo>();

			m_DiscoveredSection.Enter();

			try
			{
				DateTime cutoff = IcdEnvironment.GetLocalTime() - TimeSpan.FromMilliseconds(TIMEOUT_DURATION);
				IEnumerable<CoreDiscoveryInfo> items =
					m_Discovered.Where(kvp => kvp.Value.Discovered <= cutoff)
					            .Select(kvp => kvp.Value);

				dropped.AddRange(items);
				m_Discovered.RemoveAll(dropped.Select(d => d.Id));
			}
			finally
			{
				m_DiscoveredSection.Leave();
			}

			foreach (CoreDiscoveryInfo item in dropped)
				OnCoreLost.Raise(this, new CoreDiscoveryInfoEventArgs(item));
		}

		#region Repeater Callbacks

		/// <summary>
		/// Update the broadcaster data before the broadcast is sent.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected override void BroadcasterOnBroadcasting(object sender, EventArgs e)
		{
			base.BroadcasterOnBroadcasting(sender, e);

			Broadcaster.UpdateData(new CoreDiscoveryData(Core));
		}

		/// <summary>
		/// Handle received data from the broadcaster.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected override void BroadcasterOnBroadcastReceived(object sender, BroadcastEventArgs<CoreDiscoveryData> e)
		{
			base.BroadcasterOnBroadcastReceived(sender, e);

			CoreDiscoveryInfo info = new CoreDiscoveryInfo(e.Data);

			m_DiscoveredSection.Enter();

			try
			{
				CoreDiscoveryInfo existing = m_Discovered.GetDefault(info.Id, null);

				// Check for conflicts
				if (existing != null && existing.Conflicts(info))
				{
					ServiceProvider.GetService<ILoggerService>()
								   .AddEntry(eSeverity.Warning, "{0} - Conflict between Core Id={1} at {2} and {3}",
											 GetType().Name, info.Id, info.Source, existing.Source);
					return;
				}

				m_Discovered[info.Id] = info;

				// Don't raise event if the core was already discovered
				if (existing != null)
					return;
			}
			finally
			{
				m_DiscoveredSection.Leave();
			}

			OnCoreDiscovered.Raise(this, new CoreDiscoveryInfoEventArgs(info));
		}

		#endregion
	}
}