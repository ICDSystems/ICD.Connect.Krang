using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Broadcast.Broadcasters;
using ICD.Connect.Protocol.Network.Utils;
using ICD.Connect.Protocol.Ports;
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

		/// <summary>
		/// Raised when a remote core is discovered.
		/// </summary>
		public event EventHandler<GenericEventArgs<HostSessionInfo>> OnCoreDiscovered;

		/// <summary>
		/// Raised when a core is lost due to timeout or conflict.
		/// </summary>
		public event EventHandler<GenericEventArgs<HostSessionInfo>> OnCoreLost; 

		private readonly Dictionary<int, CoreDiscoveryInfo> m_Discovered;
		private readonly Dictionary<HostSessionInfo, RemoteCore> m_RemoteCores;
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
			m_RemoteCores = new Dictionary<HostSessionInfo, RemoteCore>();
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

		#region Private Methods

		/// <summary>
		/// Called periodically to cull any cores that have timed out.
		/// </summary>
		private void TimeoutCallback()
		{
			CoreDiscoveryInfo[] dropped;

			m_DiscoveredSection.Enter();

			try
			{
				DateTime cutoff = IcdEnvironment.GetLocalTime() - TimeSpan.FromMilliseconds(TIMEOUT_DURATION);

				dropped =
					m_Discovered.Where(kvp => kvp.Value.DiscoveryTime <= cutoff)
					            .Select(kvp => kvp.Value)
								.ToArray();
			}
			finally
			{
				m_DiscoveredSection.Leave();
			}

			foreach (CoreDiscoveryInfo item in dropped)
				RemoveCore(item);
		}

		private void LazyLoadRemoteCore(CoreDiscoveryInfo info)
		{
			bool discovered;

			m_DiscoveredSection.Enter();

			try
			{
				CoreDiscoveryInfo existing;
				discovered = !m_Discovered.TryGetValue(info.Id, out existing);
				if (discovered)
					Logger.AddEntry(eSeverity.Informational, "Core {0} discovered {1} {2}", info.Id, info.Source, info.DiscoveryTime);

				// Update discovery time even if we already know about the core.
				m_Discovered[info.Id] = info;

				if (m_RemoteCores.ContainsKey(info.Source))
					return;

				RemoteCore remoteCore = new RemoteCore(m_Core, info.Source);
				m_RemoteCores.Add(info.Source, remoteCore);

				// Query known originators
				remoteCore.Initialize();
			}
			finally
			{
				m_DiscoveredSection.Leave();
			}

			if (discovered)
				OnCoreDiscovered.Raise(this, new GenericEventArgs<HostSessionInfo>(info.Source));
		}

		private void RemoveCore(CoreDiscoveryInfo item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			m_DiscoveredSection.Enter();

			try
			{
				if (m_Discovered.Remove(item.Id))
					Logger.AddEntry(eSeverity.Warning, "Core {0} lost {1} {2}", item.Id, item.Source, item.DiscoveryTime);

				RemoteCore remoteCore;
				if (!m_RemoteCores.TryGetValue(item.Source, out remoteCore))
					return;

				m_RemoteCores.Remove(item.Source);
				remoteCore.Dispose();
			}
			finally
			{
				m_DiscoveredSection.Leave();
			}

			OnCoreLost.Raise(this, new GenericEventArgs<HostSessionInfo>(item.Source));
		}

		#endregion

		#region Repeater Callbacks

		/// <summary>
		/// Update the broadcaster data before the broadcast is sent.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected override void BroadcasterOnBroadcasting(object sender, EventArgs e)
		{
			base.BroadcasterOnBroadcasting(sender, e);

			Broadcaster.SetBroadcastData(CoreDiscoveryData.ForCore(m_Core));
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
			if (e.Data.HostSession.Host.IsLocalHost &&
			    e.Data.HostSession.Host.Port == NetworkUtils.GetBroadcastPortForSystem(BroadcastManager.SystemId))
				return;

			CoreDiscoveryInfo info = new CoreDiscoveryInfo(e.Data);
			CoreDiscoveryInfo existing = m_DiscoveredSection.Execute(() => m_Discovered.GetDefault(info.Id, null));

			// Check for conflicts
			if (existing != null && existing.Conflicts(info))
				RemoveCore(existing);

			LazyLoadRemoteCore(info);
		}

		#endregion
	}
}