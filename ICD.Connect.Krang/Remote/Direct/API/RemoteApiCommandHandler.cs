using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API;
using ICD.Connect.Krang.Remote.Broadcast.CoreDiscovery;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Krang.Remote.Direct.API
{
	/// <summary>
	/// The RemoteApiCommandHandler manages the communication between remote cores and the local API.
	/// </summary>
	public sealed class RemoteApiCommandHandler : AbstractMessageHandler<RemoteApiMessage, RemoteApiReply>
	{
		private readonly BiDictionary<HostSessionInfo, ApiRequestor> m_Requestors;
		private readonly SafeCriticalSection m_RequestorsSection;
		private readonly CoreDiscoveryBroadcastHandler m_CoreDiscovery;

		/// <summary>
		/// Constructor.
		/// </summary>
		public RemoteApiCommandHandler(CoreDiscoveryBroadcastHandler coreDiscovery)
		{
			m_Requestors = new BiDictionary<HostSessionInfo, ApiRequestor>();
			m_RequestorsSection = new SafeCriticalSection();

			m_CoreDiscovery = coreDiscovery;
			Subscribe(m_CoreDiscovery);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			Unsubscribe(m_CoreDiscovery);

			m_RequestorsSection.Enter();

			try
			{
				foreach (HostSessionInfo key in m_Requestors.Keys.ToArray(m_Requestors.Count))
					DisposeRequestor(key);
			}
			finally
			{
				m_RequestorsSection.Leave();
			}
		}

		#region Methods

		/// <summary>
		/// Handles the message receieved from the remote core.
		/// </summary>
		/// <param name="message"></param>
		/// <returns>Returns an AbstractMessage as a reply, or null for no reply</returns>
		public override RemoteApiReply HandleMessage(RemoteApiMessage message)
		{
			ApiRequestor requestor = LazyLoadRequestor(message.MessageFrom);

			ApiHandler.HandleRequest(requestor, message.Command);
			return new RemoteApiReply {Command = message.Command};
		}

		#endregion

		#region Private Methods

		private ApiRequestor LazyLoadRequestor(HostSessionInfo remoteEndpoint)
		{
			m_RequestorsSection.Enter();

			try
			{
				ApiRequestor requestor;
				if (!m_Requestors.TryGetValue(remoteEndpoint, out requestor))
				{
					requestor = new ApiRequestor {Name = remoteEndpoint.ToString()};

					m_Requestors.Add(remoteEndpoint, requestor);
					Subscribe(requestor);
				}

				return requestor;
			}
			finally
			{
				m_RequestorsSection.Leave();
			}
		}

		private void DisposeRequestor(HostSessionInfo remoteEndpoint)
		{
			m_RequestorsSection.Enter();

			try
			{
				ApiRequestor requestor;
				if (!m_Requestors.TryGetValue(remoteEndpoint, out requestor))
					return;

				Unsubscribe(requestor);
				m_Requestors.RemoveKey(remoteEndpoint);

				// Remove the requestor from the API
				ApiHandler.UnsubscribeAll(requestor);
			}
			finally
			{
				m_RequestorsSection.Leave();
			}
		}

		#endregion

		#region Core Discovery Callbacks

		/// <summary>
		/// Subscribe to core discovery events.
		/// </summary>
		/// <param name="coreDiscovery"></param>
		private void Subscribe(CoreDiscoveryBroadcastHandler coreDiscovery)
		{
			coreDiscovery.OnCoreLost += CoreDiscoveryOnCoreLost;
		}

		/// <summary>
		/// Unsubscribe from core discovery events.
		/// </summary>
		/// <param name="coreDiscovery"></param>
		private void Unsubscribe(CoreDiscoveryBroadcastHandler coreDiscovery)
		{
			coreDiscovery.OnCoreLost -= CoreDiscoveryOnCoreLost;
		}

		/// <summary>
		/// Called when a remote core is lost.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CoreDiscoveryOnCoreLost(object sender, GenericEventArgs<HostSessionInfo> eventArgs)
		{
			DisposeRequestor(eventArgs.Data);
		}

		#endregion

		#region Requestor Callbacks

		private void Subscribe(ApiRequestor requestor)
		{
			requestor.OnApiFeedback += RequestorOnApiFeedback;
		}

		private void Unsubscribe(ApiRequestor requestor)
		{
			requestor.OnApiFeedback -= RequestorOnApiFeedback;
		}

		private void RequestorOnApiFeedback(object sender, ApiClassInfoEventArgs eventArgs)
		{
			HostSessionInfo hostInfo = m_RequestorsSection.Execute(() => m_Requestors.GetKey(sender as ApiRequestor));

			RemoteApiReply reply = new RemoteApiReply
			{
				MessageTo = hostInfo,
				Command = eventArgs.Data
			};

			RaiseReply(reply);
		}

		#endregion
	}
}
