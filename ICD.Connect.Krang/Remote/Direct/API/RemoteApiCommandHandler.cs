using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API;
using ICD.Connect.Protocol.Network.Direct;

namespace ICD.Connect.Krang.Remote.Direct.API
{
	/// <summary>
	/// The RemoteApiCommandHandler receives 
	/// </summary>
	public sealed class RemoteApiCommandHandler : AbstractMessageHandler<RemoteApiMessage, RemoteApiReply>
	{
		private readonly BiDictionary<uint, ApiRequestor> m_Requestors;
		private readonly SafeCriticalSection m_RequestorsSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public RemoteApiCommandHandler()
		{
			m_Requestors = new BiDictionary<uint, ApiRequestor>();
			m_RequestorsSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			m_RequestorsSection.Enter();

			try
			{
				foreach (uint key in m_Requestors.Keys.ToArray(m_Requestors.Count))
					DisposeRequestor(key);
			}
			finally
			{
				m_RequestorsSection.Leave();
			}
		}

		/// <summary>
		/// Handles the message receieved
		/// </summary>
		/// <param name="message"></param>
		/// <returns>Returns an AbstractMessage as a reply, or null for no reply</returns>
		public override RemoteApiReply HandleMessage(RemoteApiMessage message)
		{
			ApiRequestor requestor = LazyLoadRequestor(message.ClientId);

			ApiHandler.HandleRequest(requestor, message.Command);
			return new RemoteApiReply {Command = message.Command};
		}

		private ApiRequestor LazyLoadRequestor(uint clientId)
		{
			m_RequestorsSection.Enter();

			try
			{
				ApiRequestor requestor;
				if (!m_Requestors.TryGetValue(clientId, out requestor))
				{
					requestor = new ApiRequestor();

					m_Requestors.Add(clientId, requestor);

					Subscribe(requestor);
				}

				return requestor;
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

				m_Requestors.RemoveKey(clientId);
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
			uint clientId = m_RequestorsSection.Execute(() => m_Requestors.GetKey(sender as ApiRequestor));

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
