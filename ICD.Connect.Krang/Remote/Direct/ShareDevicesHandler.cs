using System.Collections.Generic;
using System.Linq;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Extensions;
using ICD.Connect.Krang.Core;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Endpoints.Groups;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Routing.Extensions;
using ICD.Connect.Routing.Mock.Destination;
using ICD.Connect.Routing.Mock.Source;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote.Direct
{
	public sealed class ShareDevicesHandler : AbstractMessageHandler<ShareDevicesMessage>
	{
		private readonly ICore m_Core;

		public ShareDevicesHandler()
		{
			m_Core = ServiceProvider.GetService<ICore>();
		}

		public override AbstractMessage HandleMessage(ShareDevicesMessage message)
		{
			RemoteSwitcher switcher = m_Core.GetDevices().GetChildren<RemoteSwitcher>()
			                                .SingleOrDefault(rs => rs.HasHostInfo && rs.HostInfo == message.MessageFrom);
			if (switcher == null)
				return null;

			AddSources(message, switcher);
			AddDestinations(message, switcher);
			foreach (IDestinationGroup destinationGroup in message.DestinationGroups)
			{
				destinationGroup.Remote = true;
				m_Core.GetRoutingGraph().DestinationGroups.AddChild(destinationGroup);
			}

			return null;
		}

		private void AddSources(ShareDevicesMessage message, RemoteSwitcher switcher)
		{
			List<ISource> newSources = message.Sources.Where(source => m_Core.GetRoutingGraph().Sources.AddChild(source)).ToList();
			foreach (ISource source in newSources)
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Informational, "Received Source(Id:{0}, Device:{1}, output:{2} from Host {3}",
				                         source.Id, source.Endpoint.Device,
				                         source.Endpoint.Address, message.MessageFrom);
				source.Remote = true;
			}

			foreach (ISource source in message.Sources.Distinct())
			{
				// Get the device or create it if it doesn't exist
				IDevice sourceDevice = m_Core.GetDevices().ContainsChild(source.Endpoint.Device)
										   ? m_Core.GetDevices().GetChild(source.Endpoint.Device)
					                       : null;

				if (sourceDevice == null)
				{
					MockSourceDevice newSourceDevice = new MockSourceDevice
					{
						Id = source.Endpoint.Device,
						Name = "Remote Source Device"
					};
					newSourceDevice.Controls.Clear();
					newSourceDevice.AddSourceControl(source.Endpoint.Control);

					sourceDevice = newSourceDevice;
					m_Core.GetDevices().AddChild(sourceDevice);
				}

				// Add connections to the remote switcher
				List<Connection> connections = m_Core.GetRoutingGraph().Connections.ToList();
				foreach (ConnectorInfo connector in message.SourceConnections[source.Id])
				{
					if (!connections.Any(c => c.Source.Device == source.Endpoint.Device &&
					                          c.Source.Control == source.Endpoint.Control &&
					                          c.Source.Address == connector.Address &&
					                          c.ConnectionType == connector.ConnectionType))
					{
						connections.Add(new Connection(
							                MathUtils.Clamp(connections.Max(c => c.Id) + 1, ushort.MaxValue / 2, ushort.MaxValue),
							                source.Endpoint.Device,
							                source.Endpoint.Control,
							                connector.Address,
							                switcher.Id,
							                switcher.SwitcherControl.Id,
							                MathUtils.Clamp(connections.Max(c => c.Destination.Address) + 1, ushort.MaxValue / 2,
							                                ushort.MaxValue),
							                connector.ConnectionType));
					}
				}

				m_Core.GetRoutingGraph().Connections.SetConnections(connections);

				// Set outputs on MockSource if applicable
				MockSourceDevice mockSource = sourceDevice as MockSourceDevice;
				if (mockSource != null)
				{
					mockSource.Controls.Clear();
					mockSource.AddSourceControl(source.Endpoint.Control);
					mockSource.Controls
					          .GetControl<MockRouteSourceControl>(source.Endpoint.Control)
					          .SetOutputs(message.SourceConnections[source.Id]);
				}
			}
		}

		private void AddDestinations(ShareDevicesMessage message, RemoteSwitcher switcher)
		{
			List<IDestination> newDestinations =
				message.Destinations.Where(destination => m_Core.GetRoutingGraph().Destinations.AddChild(destination)).ToList();
			foreach (IDestination destination in newDestinations)
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Informational, "Received Destination(Id:{0}, Device:{1}, Input:{2} from Host {3}",
				                         destination.Id,
				                         destination.Endpoint.Device, destination.Endpoint.Address, message.MessageFrom);
				destination.Remote = true;
			}

			foreach (IDestination destination in message.Destinations.Distinct())
			{
				// Get the device or create it if it doesn't exist
				IDevice destinationDevice = m_Core.GetDevices().ContainsChild(destination.Endpoint.Device)
												? m_Core.GetDevices().GetChild(destination.Endpoint.Device)
					                            : null;

				if (destinationDevice == null)
				{
					MockDestinationDevice newDestinationDevice = new MockDestinationDevice
					{
						Id = destination.Endpoint.Device,
						Name = "Remote Destination Device"
					};
					newDestinationDevice.Controls.Clear();
					newDestinationDevice.AddDestinationControl(destination.Endpoint.Control);

					destinationDevice = newDestinationDevice;
					m_Core.GetDevices().AddChild(destinationDevice);
				}

				// Add connections to the remote switcher
				List<Connection> connections = m_Core.GetRoutingGraph().Connections.ToList();
				foreach (ConnectorInfo connector in message.DestinationConnections[destination.Id])
				{
					if (!connections.Any(c => c.Destination.Device == destination.Endpoint.Device &&
					                          c.Destination.Control == destination.Endpoint.Control &&
					                          c.Destination.Address == connector.Address &&
					                          c.ConnectionType == connector.ConnectionType))
					{
						connections.Add(new Connection(
							                MathUtils.Clamp(connections.Max(c => c.Id) + 1, ushort.MaxValue / 2, ushort.MaxValue),
							                switcher.Id,
							                switcher.SwitcherControl.Id,
							                MathUtils.Clamp(connections.Max(c => c.Source.Address) + 1, ushort.MaxValue / 2, ushort.MaxValue),
							                destination.Endpoint.Device,
							                destination.Endpoint.Control,
							                connector.Address,
							                connector.ConnectionType));
					}
				}

				m_Core.GetRoutingGraph().Connections.SetConnections(connections);

				// Set inputs on MockDestination if applicable
				MockDestinationDevice mockDestination = destinationDevice as MockDestinationDevice;
				if (mockDestination != null)
				{
					mockDestination.Controls.Clear();
					mockDestination.AddDestinationControl(destination.Endpoint.Control);
					mockDestination.Controls
					               .GetControl<MockRouteDestinationControl>(destination.Endpoint.Control)
					               .SetInputs(message.DestinationConnections[destination.Id]);
				}
			}
		}
	}
}
