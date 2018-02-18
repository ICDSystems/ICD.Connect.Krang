using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Krang.Core;
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

		private readonly Dictionary<int, CoreDiscoveryInfo> m_Discovered;
		private readonly CoreProxyCollection m_CoreProxyCollection;
		private readonly SafeCriticalSection m_DiscoveredSection;
		private readonly SafeTimer m_TimeoutTimer;

		public ICore Core { get { return ServiceProvider.GetService<ICore>(); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public CoreDiscoveryBroadcastHandler()
		{
			m_Discovered = new Dictionary<int, CoreDiscoveryInfo>();
			m_CoreProxyCollection = new CoreProxyCollection();
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
					m_Discovered.Where(kvp => kvp.Value.Discovered <= cutoff)
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

		private void AddCore(CoreDiscoveryInfo info)
		{
			m_DiscoveredSection.Enter();

			try
			{
				CoreDiscoveryInfo existing = m_Discovered.GetDefault(info.Id, null);

				// Update discovery time even if we already know about the core.
				m_Discovered[info.Id] = info;

				if (existing != null)
					return;

				IcdConsole.PrintLine("Core {0} discovered {1} {2}", info.Id, info.Source, info.Discovered);

				CoreProxy proxy = new CoreProxy
				{
					Id = info.Id,
					Name = info.Name
				};

				m_CoreProxyCollection.Add(proxy);
				proxy.SetHostInfo(info.Source);
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

				IcdConsole.PrintLine("Core {0} lost {1} {2}", item.Id, item.Source, item.Discovered);

				CoreProxy proxy;
				if (!m_CoreProxyCollection.TryGetProxy(item.Id, out proxy))
					return;

				m_CoreProxyCollection.Remove(item.Id);
				proxy.Dispose();
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

				AddCore(info);
			}
			finally
			{
				m_DiscoveredSection.Leave();
			}
		}

		#endregion
	}
}