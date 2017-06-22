using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.StaticRoutes;

namespace ICD.Connect.Krang.Routing.StaticRoutes
{
	public sealed class StaticRoutesCollection : IStaticRoutesCollection
	{
		private readonly Dictionary<int, StaticRoute> m_StaticRoutes;
		private readonly SafeCriticalSection m_StaticRoutesSection;

		/// <summary>
		/// Mapping each switcher to the static routes they are used in.
		/// </summary>
		private readonly Dictionary<IRouteSwitcherControl, IcdHashSet<StaticRoute>> m_SwitcherStaticRoutes;

		private readonly RoutingGraph m_RoutingGraph;

		/// <summary>
		/// Gets the number of static routes in the collection.
		/// </summary>
		public int Count { get { return m_StaticRoutesSection.Execute(() => m_StaticRoutes.Count); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="routingGraph"></param>
		public StaticRoutesCollection(RoutingGraph routingGraph)
		{
			m_RoutingGraph = routingGraph;

			m_StaticRoutes = new Dictionary<int, StaticRoute>();
			m_StaticRoutesSection = new SafeCriticalSection();
			m_SwitcherStaticRoutes = new Dictionary<IRouteSwitcherControl, IcdHashSet<StaticRoute>>();
		}

		#region Methods

		/// <summary>
		/// Gets all of the static routes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<StaticRoute> GetStaticRoutes()
		{
			return m_StaticRoutesSection.Execute(() => m_StaticRoutes.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Clears and sets the static routes.
		/// </summary>
		/// <param name="staticRoutes"></param>
		public void SetStaticRoutes(IEnumerable<StaticRoute> staticRoutes)
		{
			m_StaticRoutesSection.Enter();

			try
			{
				m_StaticRoutes.Clear();
				m_StaticRoutes.AddRange(staticRoutes, c => c.Id);

				UpdateStaticRoutes();
			}
			finally
			{
				m_StaticRoutesSection.Leave();
			}
		}

		/// <summary>
		/// Clears the static routes.
		/// </summary>
		public void Clear()
		{
			SetStaticRoutes(Enumerable.Empty<StaticRoute>());
		}

		/// <summary>
		/// Rebuilds the mapping of switchers to static routes, subscribes to switcher feedback and
		/// establishes the static routes.
		/// </summary>
		public void UpdateStaticRoutes()
		{
			m_StaticRoutesSection.Enter();

			try
			{
				// Clear the old lookup
				foreach (IRouteSwitcherControl switcher in m_SwitcherStaticRoutes.Keys.ToArray())
					m_SwitcherStaticRoutes.Remove(switcher);

				// Build the new lookup
				foreach (StaticRoute staticRoute in GetStaticRoutes())
				{
					foreach (IRouteSwitcherControl switcher in GetSwitcherDevices(staticRoute))
					{
						if (!m_SwitcherStaticRoutes.ContainsKey(switcher))
							m_SwitcherStaticRoutes[switcher] = new IcdHashSet<StaticRoute>();
						m_SwitcherStaticRoutes[switcher].Add(staticRoute);
					}

					// Initialize this static route
					Route(staticRoute);
				}
			}
			finally
			{
				m_StaticRoutesSection.Leave();
			}
		}

		/// <summary>
		/// Re-applies the static routes that include the given switcher.
		/// </summary>
		/// <param name="switcher"></param>
		public void ReApplyStaticRoutesForSwitcher(IRouteSwitcherControl switcher)
		{
			m_StaticRoutesSection.Enter();

			try
			{
				IcdHashSet<StaticRoute> staticRoutes;
				if (m_SwitcherStaticRoutes.TryGetValue(switcher, out staticRoutes))
					Route(staticRoutes);
			}
			finally
			{
				m_StaticRoutesSection.Leave();
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Routes each of the static routes in the given sequence.
		/// </summary>
		/// <param name="staticRoutes"></param>
		private void Route(IEnumerable<StaticRoute> staticRoutes)
		{
			staticRoutes.ForEach(Route);
		}

		/// <summary>
		/// Routes the path described by the given static route.
		/// </summary>
		/// <param name="staticRoute"></param>
		private void Route(StaticRoute staticRoute)
		{
			IEnumerable<Connection> connectionsEnumerable = GetConnections(staticRoute);
			Connection[] connections = connectionsEnumerable as Connection[] ?? connectionsEnumerable.ToArray();

			IcdHashSet<Connection> visited = new IcdHashSet<Connection>();

			// Looping over the connections to see where they share switchers
			foreach (Connection current in connections)
			{
				visited.Add(current);

				foreach (Connection other in connections.Where(c => !visited.Contains(c)))
				{
					IRouteSwitcherControl switcher;
					int input;
					int output;

					// Connections meet at destination
					if (current.Destination.Device == other.Source.Device)
					{
						switcher = m_RoutingGraph.GetDestinationControl(current) as IRouteSwitcherControl;
						input = current.Destination.Address;
						output = other.Source.Address;
					}
						// Connections meet at source
					else if (current.Source.Device == other.Destination.Device)
					{
						switcher = m_RoutingGraph.GetSourceControl(current) as IRouteSwitcherControl;
						output = current.Source.Address;
						input = other.Destination.Address;
					}
					else
					{
						continue;
					}

					// Force this route. Static routes don't care about ownership.
					if (switcher != null)
						switcher.Route(input, output, staticRoute.ConnectionType);
				}
			}
		}

		/// <summary>
		/// Gets the switchers for the given static route.
		/// </summary>
		/// <param name="staticRoute"></param>
		/// <returns></returns>
		private IEnumerable<IRouteSwitcherControl> GetSwitcherDevices(StaticRoute staticRoute)
		{
			return GetDevices(staticRoute).OfType<IRouteSwitcherControl>();
		}

		/// <summary>
		/// Gets the devices for the given static route.
		/// </summary>
		/// <param name="staticRoute"></param>
		/// <returns></returns>
		private IEnumerable<IRouteControl> GetDevices(StaticRoute staticRoute)
		{
			return GetConnections(staticRoute).SelectMany(c => m_RoutingGraph.GetControls(c))
			                                  .Distinct();
		}

		/// <summary>
		/// Gets the connections for the given static route.
		/// </summary>
		/// <param name="staticRoute"></param>
		/// <returns></returns>
		private IEnumerable<Connection> GetConnections(StaticRoute staticRoute)
		{
			m_StaticRoutesSection.Enter();

			try
			{
				return staticRoute.GetConnections()
				                  .Select(c => m_RoutingGraph.Connections.GetConnection(c))
				                  .ToArray();
			}
			finally
			{
				m_StaticRoutesSection.Leave();
			}
		}

		#endregion

		public IEnumerator<StaticRoute> GetEnumerator()
		{
			m_StaticRoutesSection.Enter();

			try
			{
				return m_StaticRoutes.OrderValuesByKey()
				                     .ToList()
				                     .GetEnumerator();
			}
			finally
			{
				m_StaticRoutesSection.Leave();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
