using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.Extensions;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote.Direct.RouteDevices
{
	public sealed class RouteDevicesHandler : AbstractMessageHandler<RouteDevicesMessage>
	{
		private readonly ICore m_Core;

		private readonly List<RouteDevicesMessage> m_PendingMessages;
		private readonly SafeCriticalSection m_PendingMessagesSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public RouteDevicesHandler()
		{
			m_Core = ServiceProvider.GetService<ICore>();
			m_Core.GetRoutingGraph().OnRouteFinished += RoutingGraphOnOnRouteFinished;

			m_PendingMessages = new List<RouteDevicesMessage>();
			m_PendingMessagesSection = new SafeCriticalSection();
		}

		protected override AbstractMessage HandleMessage(RouteDevicesMessage message)
		{
			m_PendingMessagesSection.Execute(() => m_PendingMessages.Add(message));
			m_Core.GetRoutingGraph().Route(message.Operation);

			return null;
		}

		private void RoutingGraphOnOnRouteFinished(object sender, RouteFinishedEventArgs args)
		{
			RouteDevicesMessage message;

			m_PendingMessagesSection.Enter();

			try
			{
				message = m_PendingMessages.SingleOrDefault(m => m.Operation.Id == args.Route.Id);
				if (message == null)
					return;

				m_PendingMessages.Remove(message);
			}
			finally
			{
				m_PendingMessagesSection.Leave();
			}

			ServiceProvider.GetService<DirectMessageManager>()
			               .Respond(message.ClientId, message.MessageId, new GenericMessage<bool> {Value = args.Success});
		}
	}
}
