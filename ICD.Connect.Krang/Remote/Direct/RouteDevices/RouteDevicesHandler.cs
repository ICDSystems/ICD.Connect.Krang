using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.Extensions;
using ICD.Connect.Routing.PathFinding;
using ICD.Connect.Routing.RoutingGraphs;

namespace ICD.Connect.Krang.Remote.Direct.RouteDevices
{
	public sealed class RouteDevicesHandler : AbstractMessageHandler
	{
		private readonly Dictionary<Guid, Message> m_PendingMessages;
		private readonly SafeCriticalSection m_PendingMessagesSection;

		private IRoutingGraph m_SubscribedRoutingGraph;

		/// <summary>
		/// Gets the message type that this handler is expecting.
		/// </summary>
		public override Type MessageType { get { return typeof(RouteDevicesData); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public RouteDevicesHandler()
		{
			m_PendingMessages = new Dictionary<Guid, Message>();
			m_PendingMessagesSection = new SafeCriticalSection();

			Core.Originators.OnChildrenChanged += OriginatorsOnChildrenChanged;
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
			Core.TryGetRoutingGraph(out output);
			return output;
		}

		public override Message HandleMessage(Message message)
		{
			RouteOperation operation = message.Data as RouteOperation;
			if (operation == null)
				return null;

			m_PendingMessagesSection.Execute(() => m_PendingMessages.Add(operation.Id, message));

			IRoutingGraph graph = m_SubscribedRoutingGraph;
			if (graph == null)
				return null;

			IPathFinder pathFinder = new DefaultPathFinder(m_SubscribedRoutingGraph, operation.RoomId);

			IEnumerable<ConnectionPath> paths =
				PathBuilder.FindPaths()
						   .ForOperation(operation)
						   .With(pathFinder);

			graph.RoutePaths(paths, operation.RoomId);

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
			Message message;

			m_PendingMessagesSection.Enter();

			try
			{
				if (!m_PendingMessages.TryGetValue(args.Route.Id, out message))
					return;

				m_PendingMessages.Remove(args.Route.Id);
			}
			finally
			{
				m_PendingMessagesSection.Leave();
			}

			Message reply = Message.FromData(new RouteDevicesData {Result = args.Success});
			reply.To = message.From;

			RaiseReply(reply);
		}

		#endregion
	}
}
