using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Endpoints;
using ICD.Connect.Routing.Extensions;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote.Direct
{
	// Based on RIP (https://tools.ietf.org/html/rfc2453#section-3.9.1)
	public sealed class CostUpdateHandler : AbstractMessageHandler<CostUpdateMessage>
	{
		private const double MAX_COST = 16;

		private readonly double m_Addition =
				new Random(ServiceProvider.GetService<DirectMessageManager>().GetHostInfo().ToString().GetHashCode()).NextDouble()/100;

		private const int TIMEOUT_TIME = 120*1000;
		private const int DELETION_TIME = 120*1000;
		private const int UPDATE_TIME = 30*1000;

		private sealed class Row
		{
			public double Cost { get; set; }
			public HostInfo RouteTo { get; set; }
			public bool RouteChanged { get; set; }
			public SafeTimer Timeout { get; set; }
			public SafeTimer Deletion { get; set; }
		}

		private readonly Dictionary<int, Row> m_SourceCosts;
		private readonly Dictionary<int, Row> m_DestinationCosts;
		private readonly Dictionary<int, Row> m_DestinationGroupCosts; 
		private readonly SafeCriticalSection m_CostsCriticalSection;

		private readonly SafeTimer m_RegularUpdateTimer;
		private SafeTimer m_TriggeredUpdateCooldownTimer;
		private bool m_TriggeredUpdateAllowed = true;
		private bool m_TriggeredUpdateQueued;

		private readonly ICore m_Core;

		public CostUpdateHandler()
		{
			m_SourceCosts = new Dictionary<int, Row>();
			m_DestinationCosts = new Dictionary<int, Row>();
			m_DestinationGroupCosts = new Dictionary<int, Row>();
			m_CostsCriticalSection = new SafeCriticalSection();

			m_Core = ServiceProvider.GetService<ICore>();
			
			m_RegularUpdateTimer = new SafeTimer(SendRegularUpdate, UPDATE_TIME, UPDATE_TIME);
		}

		public override AbstractMessage HandleMessage(CostUpdateMessage message)
		{
			InitializeCostTables();

			// validate that message is from a direct neighbor
			var switcher = m_Core.Originators.GetChildren<RemoteSwitcher>().FirstOrDefault(rs => rs.HasHostInfo && rs.HostInfo == message.MessageFrom);
			if (switcher == null)
				return null;

			List<int> missingSources;
			List<int> missingDestinations;
			List<int> missingDestinationGroups;
			bool triggerUpdate = HandleCostUpdates(message.SourceCosts, message.MessageFrom, m_SourceCosts, out missingSources);
			triggerUpdate = HandleCostUpdates(message.DestinationCosts, message.MessageFrom, m_DestinationCosts, out missingDestinations) || triggerUpdate;
			triggerUpdate = HandleCostUpdates(message.DestinationGroupCosts, message.MessageFrom, m_DestinationGroupCosts, out missingDestinationGroups) || triggerUpdate;

			// loop through device ids in the costs and move devices to the remote switcher with the lowest cost
			ReplaceSourceConnections(m_SourceCosts.Where(s => s.Value.RouteChanged));
			ReplaceDestinationConnections(m_DestinationCosts.Where(d => d.Value.RouteChanged));

			if (triggerUpdate)
				QueueTriggeredUpdate();

			if (missingSources.Any() || missingDestinations.Any() || missingDestinationGroups.Any())
				ServiceProvider.GetService<DirectMessageManager>().Send(message.MessageFrom, new RequestDevicesMessage
				{
					Sources = missingSources,
					Destinations = missingDestinations,
					DestinationGroups = missingDestinationGroups
				});

			return null;
		}

		private void InitializeCostTables()
		{
			m_CostsCriticalSection.Enter();
			var hostInfo = ServiceProvider.GetService<DirectMessageManager>().GetHostInfo();

			// fill with local sources/destinations with cost 0 and no timeout
			if(m_SourceCosts.Count == 0)
				m_SourceCosts.AddRange(m_Core.GetRoutingGraph().Sources.Where(s => !s.Remote).ToDictionary(s => s.Id, s => new Row { Cost = 0, RouteTo = hostInfo }));
			if(m_DestinationCosts.Count == 0)
				m_DestinationCosts.AddRange(m_Core.GetRoutingGraph().Destinations.Where(d => !d.Remote).ToDictionary(d => d.Id, d => new Row { Cost = 0, RouteTo = hostInfo }));
			if(m_DestinationGroupCosts.Count == 0)
				m_DestinationGroupCosts.AddRange(m_Core.GetRoutingGraph().DestinationGroups.Where(d => !d.Remote).ToDictionary(d => d.Id, d => new Row { Cost = 0, RouteTo = hostInfo }));
			m_CostsCriticalSection.Leave();
		}

		private bool HandleCostUpdates(Dictionary<int, double> costs, HostInfo messageFrom, Dictionary<int, Row> table, out List<int> missing)
		{
			bool triggerUpdate = false;
			m_CostsCriticalSection.Enter();

			missing = new List<int>();
			foreach (var entry in costs)
			{
				// make sure source/dest is valid
				if (((table == m_SourceCosts && !m_Core.GetRoutingGraph().Sources.ContainsChild(entry.Key)) ||
					(table == m_DestinationCosts && !m_Core.GetRoutingGraph().Destinations.ContainsChild(entry.Key)) ||
					(table == m_DestinationGroupCosts && !m_Core.GetRoutingGraph().DestinationGroups.ContainsChild(entry.Key))) && entry.Value < MAX_COST)
				{
					missing.Add(entry.Key);
					continue;
				}

				// add cost of local routing graph, up to maximum
				double cost = MathUtils.Clamp(entry.Value + CalculateSourceCost(entry.Key), 0, MAX_COST);

				// if source exists
				if(table.ContainsKey(entry.Key))
				{
					var row = table[entry.Key];

					// if from same host, restart timeout timer
					if (row.RouteTo == messageFrom)
					{
						row.Deletion.Stop();
						row.Timeout.Reset(TIMEOUT_TIME);
					}

					if ((row.RouteTo == messageFrom && Math.Abs(row.Cost - cost) > 0.0000000001) || cost < row.Cost)
					{
						if(row.RouteTo != messageFrom)
							ServiceProvider.TryGetService<ILoggerService>().AddEntry(eSeverity.Informational, "Replacing {0} with {1}", row.RouteTo, messageFrom);
						row.RouteTo = messageFrom;
						row.Cost = cost;
						row.Deletion.Stop();
						row.Timeout.Reset(TIMEOUT_TIME);
						row.RouteChanged = true;

						if (cost >= MAX_COST)
						{
							row.Timeout.Trigger();
						}
						triggerUpdate = true;

					}
				}
				// add source to table if it isn't infinite cost
				else if (cost < MAX_COST)
				{
					table.Add(entry.Key, new Row
					{
						Cost = cost,
						RouteTo = messageFrom,
						RouteChanged = true,
						Timeout = new SafeTimer(TimeoutCallback(entry.Key, table), TIMEOUT_TIME, -1),
						Deletion = SafeTimer.Stopped(DeletionCallback(entry.Key, table))
					});
					triggerUpdate = true;
				}
			}
			m_CostsCriticalSection.Leave();
			return triggerUpdate;
		}

		private Action TimeoutCallback(int id, Dictionary<int, Row> table)
		{
			return () =>
			{
				m_CostsCriticalSection.Enter();
				if (!table.ContainsKey(id))
					return;

				ServiceProvider.TryGetService<ILoggerService>().AddEntry(eSeverity.Warning, "Remote {0} {1} has timed out or reached max cost, and will be deleted in {2} seconds",
						table == m_SourceCosts ? "Source" : "Destination", id, DELETION_TIME/1000);
				table[id].Cost = MAX_COST;
				table[id].Deletion.Reset(DELETION_TIME);
				table[id].Timeout.Stop();
				table[id].RouteChanged = true;

				if (m_SourceCosts.Union(m_DestinationCosts).Union(m_DestinationGroupCosts).Where(r => r.Value.RouteTo == table[id].RouteTo).All(r => r.Value.Cost >= MAX_COST))
				{
					ServiceProvider.TryGetService<ILoggerService>().AddEntry(eSeverity.Warning, "All sources and destinations from {0} have timed out, resetting remote switcher to discovery mode",
							table[id].RouteTo);
					var switcher =
							m_Core.Originators.GetChildren<RemoteSwitcher>().FirstOrDefault(rs => rs.HasHostInfo && rs.HostInfo == table[id].RouteTo);
					if (switcher != null)
						switcher.HostInfo = default(HostInfo);
				}

				m_CostsCriticalSection.Leave();
				QueueTriggeredUpdate();
			};
		}

		private Action DeletionCallback(int id, Dictionary<int, Row> table)
		{
			return () =>
			{
				m_CostsCriticalSection.Enter();
				if (!table.ContainsKey(id))
					return;
				
				if (table == m_SourceCosts)
					RemoveSource(id);
				else if (table == m_DestinationCosts)
					RemoveDestination(id);
				else if (table == m_DestinationGroupCosts)
					RemoveDestinationGroup(id);
				table.Remove(id);
				m_CostsCriticalSection.Leave();
			};
		}

		private void RemoveSource(int id)
		{
			var source = m_Core.GetRoutingGraph().Sources.GetChild(id);
			ServiceProvider.TryGetService<ILoggerService>().AddEntry(eSeverity.Error, "Removing remote Source(Device={0}, Output={1}) due to timeout", source.Endpoint.Device, source.Endpoint.Address);
			var connections = m_Core.GetRoutingGraph().Connections.GetConnections();
			
			// list of connections minus the ones connected to the source
			var connectionsLeft = connections.Where(c => c.Source != source.Endpoint)
			                                 .ToList();

			// remove device if no connections left to it
			if (!connectionsLeft.Any(c => c.Source.Device == source.Endpoint.Device) && m_Core.Originators.ContainsChild(source.Endpoint.Device))
			{
				var device = m_Core.Originators.GetChild(source.Endpoint.Device);
				m_Core.Originators.RemoveChild(device);
			}

			m_Core.GetRoutingGraph().Connections.SetConnections(connectionsLeft);
			m_Core.GetRoutingGraph().Sources.RemoveChild(source);
		}

		private void RemoveDestination(int id)
		{
			var destination = m_Core.GetRoutingGraph().Destinations.GetChild(id);
			ServiceProvider.TryGetService<ILoggerService>().AddEntry(eSeverity.Error, "Removing remote Destination(Device={0}, Input={1}) due to timeout", destination.Endpoint.Device, destination.Endpoint.Address);
			var connections = m_Core.GetRoutingGraph().Connections.GetConnections().ToList();
			
			// list of connections minus the ones connected to the destination
			var connectionsLeft =
				connections.Where(c => c.Destination != destination.Endpoint).ToList();
			
			// remove device if no connections left to it
			if (!connectionsLeft.Any(c => c.Destination.Device == destination.Endpoint.Device) &&
				m_Core.Originators.ContainsChild(destination.Endpoint.Device))
			{
				var device = m_Core.Originators.GetChild(destination.Endpoint.Device);
				m_Core.Originators.RemoveChild(device);
			}

			m_Core.GetRoutingGraph().Connections.SetConnections(connectionsLeft);
			m_Core.GetRoutingGraph().Destinations.RemoveChild(destination);
		}

		private void RemoveDestinationGroup(int id)
		{
			if (!m_Core.GetRoutingGraph().DestinationGroups.ContainsChild(id))
				return;

			m_Core.GetRoutingGraph().DestinationGroups.RemoveChild(m_Core.GetRoutingGraph().DestinationGroups.GetChild(id));
		}

		private double CalculateSourceCost(int s)
		{
			// TODO -- calculate local routing graph cost
			return 1 + m_Addition;
		}

		private void ReplaceSourceConnections(IEnumerable<KeyValuePair<int, Row>> changes)
		{
			var connections = m_Core.GetRoutingGraph().Connections.GetConnections().ToList();
			foreach (var entry in changes)
			{
				var source = m_Core.GetRoutingGraph().Sources.GetChild(entry.Key);

				// Workaround for compiler warning
				KeyValuePair<int, Row> entry1 = entry;

				var switcher = m_Core.Originators.GetChildren<RemoteSwitcher>()
				                     .SingleOrDefault(rs => rs.HasHostInfo && rs.HostInfo == entry1.Value.RouteTo);
				if (switcher == null)
					continue;

				var remove = connections.Where(c => c.Source == source.Endpoint && (c.Destination.Device != switcher.Id || c.Destination.Control != switcher.SwitcherControl.Id))
				                        .ToList();

				connections.RemoveAll(remove);
				connections.AddRange(remove.Select(c => new Connection(
					c.Id,
					c.Source,
					new EndpointInfo(switcher.Id,
									 switcher.SwitcherControl.Id,
									 c.Destination.Address),
					c.ConnectionType)));
			}
			m_Core.GetRoutingGraph().Connections.SetConnections(connections);
		}

		private void ReplaceDestinationConnections(IEnumerable<KeyValuePair<int, Row>> changes)
		{
			var connections = m_Core.GetRoutingGraph().Connections.GetConnections().ToList();
			foreach (var entry in changes)
			{
				var destination = m_Core.GetRoutingGraph().Destinations.GetChild(entry.Key);

				// Workaround for compiler warning
				KeyValuePair<int, Row> entry1 = entry;

				var switcher =
						m_Core.Originators.GetChildren<RemoteSwitcher>().SingleOrDefault(rs => rs.HasHostInfo && rs.HostInfo == entry1.Value.RouteTo);
				if (switcher == null)
					continue;

				var remove = connections.Where(c => c.Destination == destination.Endpoint && (c.Source.Device != switcher.Id ||
										c.Source.Control != switcher.SwitcherControl.Id)).ToList();

				connections.RemoveAll(remove);
				connections.AddRange(remove.Select(c => new Connection(
					c.Id,
					new EndpointInfo(switcher.Id,
									 switcher.SwitcherControl.Id,
									 c.Source.Address),
					c.Destination,
					c.ConnectionType)));
			}
			m_Core.GetRoutingGraph().Connections.SetConnections(connections);
		}

		#region Output Processing

		private static Dictionary<int, double> TableToCostMessage(Dictionary<int, Row> table, HostInfo host)
		{
			return table.ToDictionary(s => s.Key, s => s.Value.RouteTo != host ? s.Value.Cost : MAX_COST);
		}

		private static CostUpdateMessage MakeCostUpdateMessage(Dictionary<int, Row> sources, Dictionary<int, Row> destinations, Dictionary<int, Row> destinationGroups, HostInfo host)
		{
			var message = new CostUpdateMessage
			{
				SourceCosts = TableToCostMessage(sources, host),
				DestinationCosts = TableToCostMessage(destinations, host),
				DestinationGroupCosts = TableToCostMessage(destinationGroups, host)
			};
			return message;
		}

		private void SendRegularUpdate()
		{
			InitializeCostTables();

			var dmManager = ServiceProvider.GetService<DirectMessageManager>();
			
			foreach (var host in GetHosts())
			{
				var message = MakeCostUpdateMessage(m_SourceCosts, m_DestinationCosts, m_DestinationGroupCosts, host);
				dmManager.Send(host, message);
			}
			m_RegularUpdateTimer.Reset(UPDATE_TIME + new Random().Next(-5, 5));
		}

		private void SendTriggeredUpdate()
		{
			m_TriggeredUpdateAllowed = false;

			var dmManager = ServiceProvider.GetService<DirectMessageManager>();

			m_CostsCriticalSection.Enter();

			//generate and send messages
			foreach (var host in GetHosts())
			{
				var message = MakeCostUpdateMessage(m_SourceCosts.Where(s => s.Value.RouteChanged).ToDictionary(),
						m_DestinationCosts.Where(s => s.Value.RouteChanged).ToDictionary(), 
						m_DestinationGroupCosts.Where(s => s.Value.RouteChanged).ToDictionary(), host);
				dmManager.Send(host, message);
			}

			// set route change flags to false
			foreach (var row in m_SourceCosts)
				row.Value.RouteChanged = false;
			foreach (var row in m_DestinationCosts)
				row.Value.RouteChanged = false;

			m_CostsCriticalSection.Leave();

			m_TriggeredUpdateQueued = false;
			m_TriggeredUpdateCooldownTimer = new SafeTimer(TriggeredUpdateCallback, new Random().Next(5), -1);
		}

		private void QueueTriggeredUpdate()
		{
			if (m_TriggeredUpdateAllowed)
				SendTriggeredUpdate();
			else 
				m_TriggeredUpdateQueued = true;
		}

		private void TriggeredUpdateCallback()
		{
			m_TriggeredUpdateAllowed = true;
			if (m_TriggeredUpdateQueued)
				SendTriggeredUpdate();
		}

		private IEnumerable<HostInfo> GetHosts()
		{
			return m_Core.Originators.GetChildren<RemoteSwitcher>().Where(rs => rs.HasHostInfo).Select(rs => rs.HostInfo);
		}

		protected override void Dispose(bool disposing)
		{
			m_TriggeredUpdateAllowed = false;

			if (m_RegularUpdateTimer != null)
				m_RegularUpdateTimer.Dispose();
			if (m_TriggeredUpdateCooldownTimer != null)
				m_TriggeredUpdateCooldownTimer.Dispose();

			foreach (var row in m_SourceCosts.Union(m_DestinationCosts))
			{
				if (row.Value == null)
					continue;

				if (row.Value.Deletion != null)
					row.Value.Deletion.Dispose();
				if (row.Value.Timeout != null)
					row.Value.Timeout.Dispose();
			}
		}

		#endregion
	}
}