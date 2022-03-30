using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Broadcast.Broadcasters;
using ICD.Connect.Protocol.Network.Utils;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Krang.Remote.Broadcast.CoreDiscovery
{
	/// <summary>
	/// CoreDiscoveryBroadcastHandler simply tracks the discovered core instances on the network.
	/// </summary>
	public sealed class CoreDiscoveryBroadcastHandler : AbstractBroadcastHandler<CoreDiscoveryData>
	{
		private const long DEFAULT_INTERVAL = 30 * 1000;

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
		
		private ILoggerService Logger { get { return ServiceProvider.GetService<ILoggerService>(); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public CoreDiscoveryBroadcastHandler()
		{
			m_Discovered = new Dictionary<int, CoreDiscoveryInfo>();
			m_RemoteCores = new Dictionary<HostSessionInfo, RemoteCore>();
			m_DiscoveredSection = new SafeCriticalSection();

			SetBroadcaster(new RecurringBroadcaster<CoreDiscoveryData>(DEFAULT_INTERVAL));
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnCoreDiscovered = null;
			OnCoreLost = null;

			base.Dispose();
		}

		#region Private Methods

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

				RemoteCore remoteCore = new RemoteCore(Core, info.Source);
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

			Broadcaster.SetBroadcastData(CoreDiscoveryData.ForCore(Core));
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

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public override string ConsoleName { get { return "CoreDiscovery"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp { get { return "Tracks remote cores"; } }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			List<RemoteCore> remoteCores = m_DiscoveredSection.Execute(() => m_RemoteCores.Values.ToList(m_RemoteCores.Count));
			yield return ConsoleNodeGroup.IndexNodeMap("RemoteCores", remoteCores);
		}

		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("PrintDiscoveredCores", "Prints discovered cores and their info", () => PrintDiscoveredCores());
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		private string PrintDiscoveredCores()
		{
			List<CoreDiscoveryInfo> discovered =
				m_DiscoveredSection.Execute(() => m_Discovered.Values.ToList(m_Discovered.Count));

			TableBuilder table = new TableBuilder("Id", "Name", "GUID", "Host", "DiscoveryTime");

			foreach (CoreDiscoveryInfo core in discovered)
				table.AddRow(core.Id, core.Name, core.Source.Session, core.Source.Host, core.DiscoveryTime);

			return table.ToString();

		}

		#endregion
	}
}