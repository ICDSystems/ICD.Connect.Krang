using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Krang.Cores;
using ICD.Connect.Krang.Remote.Broadcast;
using ICD.Connect.Krang.Remote.Broadcast.CoreDiscovery;
using ICD.Connect.Krang.Remote.Broadcast.OriginatorsChange;
using ICD.Connect.Krang.Remote.Broadcast.TielineDiscovery;
using ICD.Connect.Krang.Remote.Direct.API;
using ICD.Connect.Krang.Remote.Direct.CostUpdate;
using ICD.Connect.Krang.Remote.Direct.Disconnect;
using ICD.Connect.Krang.Remote.Direct.InitiateConnection;
using ICD.Connect.Krang.Remote.Direct.RequestDevices;
using ICD.Connect.Krang.Remote.Direct.RouteDevices;
using ICD.Connect.Krang.Remote.Direct.ShareDevices;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Services;

namespace ICD.Connect.Krang.Remote
{
	public sealed class InterCoreService : AbstractService<IInterCoreService, InterCoreServiceSettings>, IInterCoreService
	{
		private readonly IcdHashSet<IBroadcastHandler> m_BroadcastHandlers;
		private readonly IcdHashSet<IMessageHandler> m_MessageHandlers;

		#region Properties

		[CanBeNull]
		private static BroadcastManager BroadcastManager { get { return ServiceProvider.TryGetService<BroadcastManager>(); } }

		[CanBeNull]
		private static DirectMessageManager DirectMessageManager
		{
			get { return ServiceProvider.TryGetService<DirectMessageManager>(); }
		}

		public bool Enabled { get; private set; }

		public bool IsRunning { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public InterCoreService()
		{
			CoreDiscoveryBroadcastHandler coreDiscovery = new CoreDiscoveryBroadcastHandler();

			m_BroadcastHandlers = new IcdHashSet<IBroadcastHandler>
			{
				coreDiscovery,
				new OriginatorsChangeBroadcastHandler(),
				new TielineDiscoveryBroadcastHandler()
			};

			m_MessageHandlers = new IcdHashSet<IMessageHandler>
			{
				new InitiateConnectionHandler(),
				new ShareDevicesHandler(),
				new CostUpdateHandler(),
				new RequestDevicesHandler(),
				new DisconnectHandler(),
				new RouteDevicesHandler(),
				new RemoteApiCommandHandler(coreDiscovery)
			};

			DirectMessageManager manager = DirectMessageManager;
			if (manager == null)
				return;

			foreach (IMessageHandler handler in m_MessageHandlers)
				manager.RegisterMessageHandler(handler);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Stop();

			foreach (IBroadcastHandler handler in m_BroadcastHandlers)
				handler.Dispose();
			m_BroadcastHandlers.Clear();

			foreach (IMessageHandler handler in m_MessageHandlers)
				handler.Dispose();
			m_MessageHandlers.Clear();
		}

		#region Methods

		/// <summary>
		/// Starts inter-core communication.
		/// </summary>
		public void Start()
		{
			IsRunning = true;

			BroadcastManager broadcastManager = BroadcastManager;
			if (broadcastManager != null)
				broadcastManager.Start();

			DirectMessageManager directMessageManager = DirectMessageManager;
			if (directMessageManager != null)
				directMessageManager.Start();
		}

		/// <summary>
		/// Stops inter-core communication.
		/// </summary>
		public void Stop()
		{
			IsRunning = false;

			BroadcastManager broadcastManager = BroadcastManager;
			if (broadcastManager != null)
				broadcastManager.Stop();

			DirectMessageManager directMessageManager = DirectMessageManager;
			if (directMessageManager != null)
				directMessageManager.Stop();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sets the initial broadcast addresses for inter-core communication.
		/// </summary>
		/// <param name="addresses"></param>
		public void SetBroadcastAddresses([NotNull] IEnumerable<string> addresses)
		{
			if (addresses == null)
				throw new ArgumentNullException("addresses");

			BroadcastManager manager = BroadcastManager;
			if (manager != null)
				manager.SetBroadcastAddresses(addresses);
		}

		/// <summary>
		/// Gets the current broadcast addresses for inter-core communication.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetBroadcastAddresses()
		{
			BroadcastManager manager = BroadcastManager;
			return manager == null
				       ? Enumerable.Empty<string>()
				       : manager.GetBroadcastAddresses();
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"/><param name="factory"/>
		protected override void ApplySettingsFinal(InterCoreServiceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);
		
			IEnumerable<string> addresses = settings.GetAddresses();
			SetBroadcastAddresses(addresses);

			Enabled = settings.Enabled;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Stop();
			SetBroadcastAddresses(Enumerable.Empty<string>());
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(InterCoreServiceSettings settings)
		{
			base.CopySettingsFinal(settings);
		
			settings.Enabled = Enabled;

			IEnumerable<string> addresses = GetBroadcastAddresses();
			settings.SetAddresses(addresses);
		}

		/// <summary>
		/// Override to add actions on StartSettings
		/// This should be used to start communications with devices and perform initial actions
		/// </summary>
		protected override void StartSettingsFinal()
		{
			base.StartSettingsFinal();

			if (Enabled)
				Start();
			else
				Stop();
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("IsRunning", IsRunning);
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IBroadcastHandler broadcastHandler in m_BroadcastHandlers)
				yield return broadcastHandler;
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

			yield return new ConsoleCommand("Start", "Start Inter-Core Services", () => Start());
			yield return new ConsoleCommand("Stop", "Stop Inter-Core Services", () => Stop());
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
