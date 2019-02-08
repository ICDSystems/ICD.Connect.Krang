using System;
using System.Collections.Generic;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services;
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

		private static BroadcastManager BroadcastManager { get { return ServiceProvider.TryGetService<BroadcastManager>(); } }

		private static DirectMessageManager DirectMessageManager
		{
			get { return ServiceProvider.TryGetService<DirectMessageManager>(); }
		}

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
				new RemoteApiCommandHandler(coreDiscovery),
				new RemoteApiResultHandler()
			};

			foreach (IMessageHandler handler in m_MessageHandlers)
				DirectMessageManager.RegisterMessageHandler(handler);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			foreach (IBroadcastHandler handler in m_BroadcastHandlers)
				handler.Dispose();
			m_BroadcastHandlers.Clear();

			foreach (IMessageHandler handler in m_MessageHandlers)
				handler.Dispose();
			m_MessageHandlers.Clear();
		}

		#region Methods

		public void Start()
		{
			BroadcastManager.Start();
			DirectMessageManager.Start();
		}

		public void Stop()
		{
			BroadcastManager.Stop();
			DirectMessageManager.Stop();
		}

		public void SetBroadcastAddresses(IEnumerable<string> addresses)
		{
			if (addresses == null)
				throw new ArgumentNullException("addresses");

			BroadcastManager.SetBroadcastAddresses(addresses);
		}

		#endregion
	}
}
