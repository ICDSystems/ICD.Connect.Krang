using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API;
using ICD.Connect.Protocol.Network.Direct;

namespace ICD.Connect.Krang.Remote.Direct.API
{
	/// <summary>
	/// The RemoteApiCommandHandler receives 
	/// </summary>
	public sealed class RemoteApiCommandHandler : AbstractMessageHandler<RemoteApiMessage, RemoteApiReply>
	{
		private readonly Dictionary<uint, ApiRequestor> m_Requestors;
		private readonly Dictionary<ApiRequestor, uint> m_RequestorClientIds;
		private readonly SafeCriticalSection m_RequestorsSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public RemoteApiCommandHandler()
		{
			m_Requestors = new Dictionary<uint, ApiRequestor>();
			m_RequestorClientIds = new Dictionary<ApiRequestor, uint>();
			m_RequestorsSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Handles the message receieved
		/// </summary>
		/// <param name="message"></param>
		/// <returns>Returns an AbstractMessage as a reply, or null for no reply</returns>
		protected override RemoteApiReply HandleMessage(RemoteApiMessage message)
		{
			ApiRequestor requestor = LazyLoadRequestor(message.ClientId);

			ApiHandler.HandleRequest(requestor, message.Command);
			return new RemoteApiReply {Command = message.Command};
		}

		/// <summary>
		/// Called to inform the message handler of a client disconnect.
		/// </summary>
		/// <param name="clientId"></param>
		public override void HandleClientDisconnect(uint clientId)
		{
			base.HandleClientDisconnect(clientId);

			DisposeRequestor(clientId);
		}

		private ApiRequestor LazyLoadRequestor(uint clientId)
		{
			m_RequestorsSection.Enter();

			try
			{
				if (!m_Requestors.ContainsKey(clientId))
				{
					ApiRequestor requestor = new ApiRequestor();

					m_Requestors[clientId] = requestor;
					m_RequestorClientIds[requestor] = clientId;

					Subscribe(requestor);
				}

				return m_Requestors[clientId];
			}
			finally
			{
				m_RequestorsSection.Leave();
			}
		}

		private void DisposeRequestor(uint clientId)
		{
			m_RequestorsSection.Enter();

			try
			{
				ApiRequestor requestor;
				if (!m_Requestors.TryGetValue(clientId, out requestor))
					return;

				Unsubscribe(requestor);

				m_Requestors.Remove(clientId);
				m_RequestorClientIds.Remove(requestor);
			}
			finally
			{
				m_RequestorsSection.Leave();
			}
		}

		#region Requestor Callbacks

		private void Subscribe(ApiRequestor requestor)
		{
			requestor.OnApiFeedback += RequestorOnOnApiFeedback;
		}

		private void Unsubscribe(ApiRequestor requestor)
		{
			requestor.OnApiFeedback -= RequestorOnOnApiFeedback;
		}

		private void RequestorOnOnApiFeedback(object sender, ApiClassInfoEventArgs eventArgs)
		{
			uint clientId = m_RequestorsSection.Execute(() => m_RequestorClientIds[sender as ApiRequestor]);

			RemoteApiReply reply = new RemoteApiReply
			{
				ClientId = clientId,
				MessageId = Guid.NewGuid(),
				Command = eventArgs.Data
			};

			RaiseReply(reply);
		}

		#endregion
	}
}
