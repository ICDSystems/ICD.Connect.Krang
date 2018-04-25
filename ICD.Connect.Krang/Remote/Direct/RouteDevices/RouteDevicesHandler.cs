using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.Extensions;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote.Direct.RouteDevices
{
	public sealed class RouteDevicesHandler : AbstractMessageHandler<RouteDevicesMessage, RouteDevicesReply>
	{
		private readonly ICore m_Core;

		private readonly List<RouteDevicesMessage> m_PendingMessages;
		private readonly SafeCriticalSection m_PendingMessagesSection;

		private IRoutingGraph m_SubscribedRoutingGraph;

		/// <summary>
		/// Constructor.
		/// </summary>
		public RouteDevicesHandler()
		{
			m_PendingMessages = new List<RouteDevicesMessage>();
			m_PendingMessagesSection = new SafeCriticalSection();

			m_Core = ServiceProvider.GetService<ICore>();
			m_Core.Originators.OnChildrenChanged += OriginatorsOnChildrenChanged;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			SetRoutingGraph(null);
		}

		private void OriginatorsOnChildrenChanged(object sender, EventArgs eventArgs)
		{
			IRoutingGraph graph = GetRoutingGraph();
			SetRoutingGraph(graph);
		}

		private void SetRoutingGraph(IRoutingGraph graph)
		{
			if (graph == m_SubscribedRoutingGraph)
				return;

			Unsubscribe(m_SubscribedRoutingGraph);
			m_SubscribedRoutingGraph = graph;
			Subscribe(m_SubscribedRoutingGraph);
		}

		[CanBeNull]
		private IRoutingGraph GetRoutingGraph()
		{
			IRoutingGraph output;
			m_Core.TryGetRoutingGraph(out output);
			return output;
		}

		public override RouteDevicesReply HandleMessage(RouteDevicesMessage message)
		{
			m_PendingMessagesSection.Execute(() => m_PendingMessages.Add(message));

			IRoutingGraph graph = m_SubscribedRoutingGraph;
			if (graph != null)
				graph.Route(message.Operation);

			return null;
		}

		#region Routing Graph Callbacks

		private void Subscribe(IRoutingGraph graph)
		{
			if (graph == null)
				return;

			graph.OnRouteFinished += RoutingGraphOnOnRouteFinished;
		}

		private void Unsubscribe(IRoutingGraph graph)
		{
			if (graph == null)
				return;

			graph.OnRouteFinished -= RoutingGraphOnOnRouteFinished;
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

			RouteDevicesReply reply = new RouteDevicesReply
			{
				Result = args.Success,
				MessageId = message.MessageId,
				ClientId = message.ClientId
			};

			RaiseReply(reply);
		}

		#endregion
	}
}
