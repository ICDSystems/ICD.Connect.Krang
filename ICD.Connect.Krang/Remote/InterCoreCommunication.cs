using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services;
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
using ICD.Connect.Settings.Cores;

namespace ICD.Connect.Krang.Remote
{
	public sealed class InterCoreCommunication : IDisposable
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

		public bool IsRunning { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="core"></param>
		public InterCoreCommunication(ICore core)
		{
			CoreDiscoveryBroadcastHandler coreDiscovery = new CoreDiscoveryBroadcastHandler(core);

			m_BroadcastHandlers = new IcdHashSet<IBroadcastHandler>
			{
				coreDiscovery,
				new OriginatorsChangeBroadcastHandler(core),
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
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
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

		public void ApplySettings(BroadcastSettings broadcastSettings)
		{
			IEnumerable<string> addresses = broadcastSettings.GetAddresses();
			SetBroadcastAddresses(addresses);

			if (broadcastSettings.Enabled)
				Start();
			else
				Stop();
		}

		public void ClearSettings()
		{
			Stop();
			SetBroadcastAddresses(Enumerable.Empty<string>());
		}

		public BroadcastSettings CopySettings()
		{
			BroadcastSettings output = new BroadcastSettings
			{
				Enabled = IsRunning
			};

			IEnumerable<string> addresses = GetBroadcastAddresses();
			output.SetAddresses(addresses);

			return output;
		}

		#endregion
	}
}
