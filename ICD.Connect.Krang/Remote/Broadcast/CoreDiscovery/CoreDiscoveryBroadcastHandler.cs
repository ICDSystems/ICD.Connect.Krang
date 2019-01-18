using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Broadcast.Broadcasters;
using ICD.Connect.Settings.Cores;

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

		private readonly Dictionary<int, CoreDiscoveryInfo> m_Discovered;
		private readonly RemoteCoreCollection m_RemoteCoreCollection;
		private readonly SafeCriticalSection m_DiscoveredSection;
		private readonly SafeTimer m_TimeoutTimer;
		private readonly ICore m_Core;

		private ILoggerService Logger { get { return ServiceProvider.GetService<ILoggerService>(); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public CoreDiscoveryBroadcastHandler(ICore core)
		{
			m_Core = core;
			m_Discovered = new Dictionary<int, CoreDiscoveryInfo>();
			m_RemoteCoreCollection = new RemoteCoreCollection();
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
			base.Dispose();

			m_TimeoutTimer.Stop();
		}

		/// <summary>
		/// Called periodically to cull any cores that have timed out.
		/// </summary>
		private void TimeoutCallback()
		{
			m_DiscoveredSection.Enter();

			try
			{
				DateTime cutoff = IcdEnvironment.GetLocalTime() - TimeSpan.FromMilliseconds(TIMEOUT_DURATION);

				CoreDiscoveryInfo[] dropped =
					m_Discovered.Where(kvp => kvp.Value.DiscoveryTime <= cutoff)
					            .Select(kvp => kvp.Value)
								.ToArray();

				foreach (CoreDiscoveryInfo item in dropped)
					RemoveCore(item);
			}
			finally
			{
				m_DiscoveredSection.Leave();
			}
		}

		[NotNull]
		private RemoteCore LazyLoadRemoteCore(CoreDiscoveryInfo info)
		{
			m_DiscoveredSection.Enter();

			try
			{
				CoreDiscoveryInfo existing;
				if (!m_Discovered.TryGetValue(info.Id, out existing))
					Logger.AddEntry(eSeverity.Informational, "Core {0} discovered {1} {2}", info.Id, info.Source, info.DiscoveryTime);

				// Update discovery time even if we already know about the core.
				m_Discovered[info.Id] = info;

				RemoteCore remoteCore;
				if (!m_RemoteCoreCollection.TryGetRemoteCore(info.Source, out remoteCore))
				{
					remoteCore = new RemoteCore(m_Core, info.Source);
					m_RemoteCoreCollection.Add(info.Source, remoteCore);

					// Query known originators
					remoteCore.Initialize();
				}

				return remoteCore;
			}
			finally
			{
				m_DiscoveredSection.Leave();
			}
		}

		private void RemoveCore(CoreDiscoveryInfo item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			m_DiscoveredSection.Enter();

			try
			{
				m_Discovered.Remove(item.Id);

				Logger.AddEntry(eSeverity.Warning, "Core {0} lost {1} {2}", item.Id, item.Source, item.DiscoveryTime);

				RemoteCore remoteCore;
				if (!m_RemoteCoreCollection.TryGetRemoteCore(item.Source, out remoteCore))
					return;

				m_RemoteCoreCollection.Remove(item.Source);
				remoteCore.Dispose();
			}
			finally
			{
				m_DiscoveredSection.Leave();
			}
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

			Broadcaster.SetBroadcastData(new CoreDiscoveryData(m_Core));
		}

		/// <summary>
		/// Handle received data from the broadcaster.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected override void BroadcasterOnBroadcastReceived(object sender, BroadcastEventArgs e)
		{
			base.BroadcasterOnBroadcastReceived(sender, e);

			// Ignore local broadcasts
			if (e.Data.Source.IsLocalHost)
				return;

			CoreDiscoveryInfo info = new CoreDiscoveryInfo(e.Data);

			m_DiscoveredSection.Enter();

			try
			{
				CoreDiscoveryInfo existing = m_Discovered.GetDefault(info.Id, null);

				// Check for conflicts
				if (existing != null && existing.Conflicts(info))
				{
					Logger.AddEntry(eSeverity.Warning, "{0} - Conflict between Core Id={1} at {2} and {3}",
					                GetType().Name, info.Id, info.Source, existing.Source);
					return;
				}

				LazyLoadRemoteCore(info);
			}
			finally
			{
				m_DiscoveredSection.Leave();
			}
		}

		#endregion
	}
}