using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Devices;
using ICD.Connect.Krang.Devices;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Routing.Extensions;
using ICD.Connect.Routing.Mock.Destination;
using ICD.Connect.Routing.Mock.Source;

namespace ICD.Connect.Krang.Remote.Direct.ShareDevices
{
	public sealed class ShareDevicesHandler : AbstractMessageHandler
	{
		/// <summary>
		/// Gets the message type that this handler is expecting.
		/// </summary>
		public override Type MessageType { get { return typeof(ShareDevicesData); } }

		public override Message HandleMessage(Message message)
		{
			ShareDevicesData data = message.Data as ShareDevicesData;
			if (data == null)
				return null;

			RemoteSwitcher switcher = Core.Originators.GetChildren<RemoteSwitcher>()
			                                .SingleOrDefault(rs => rs.HasHostInfo && rs.HostInfo == message.From);
			if (switcher == null)
				return null;

			AddSources(message, data, switcher);
			AddDestinations(message, data, switcher);

			return null;
		}

		private void AddSources(Message message, ShareDevicesData data, RemoteSwitcher switcher)
		{
			List<ISource> newSources =
				data.Sources.Where(source => Core.GetRoutingGraph().Sources.AddChild(source)).ToList();
			foreach (ISource source in newSources)
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Informational, "Received {0} from Host {1}",
										 source, message.From);
				source.Remote = true;
			}

			foreach (ISource source in data.Sources.Distinct())
			{
				// Get the device or create it if it doesn't exist
				IDevice sourceDevice = Core.Originators.ContainsChild(source.Device)
					                       ? Core.Originators.GetChild<IDevice>(source.Device)
					                       : null;

				if (sourceDevice == null)
				{
					MockSourceDevice newSourceDevice = new MockSourceDevice
					{
						Id = source.Device,
						Name = "Remote Source Device"
					};
					newSourceDevice.Controls.Clear();
					newSourceDevice.AddSourceControl(source.Control);

					sourceDevice = newSourceDevice;
					Core.Originators.AddChild(sourceDevice);
				}

				// Add connections to the remote switcher
				List<Connection> connections = Core.GetRoutingGraph().Connections.ToList();
				foreach (ConnectorInfo connector in data.SourceConnections[source.Id])
				{
					// Workaround for compiler warning
					ISource source1 = source;
					ConnectorInfo connector1 = connector;

					if (!connections.Any(c => c.Source.Device == source1.Device &&
					                          c.Source.Control == source1.Control &&
					                          c.Source.Address == connector1.Address &&
					                          c.ConnectionType == connector1.ConnectionType))
					{
						connections.Add(new Connection(
							                MathUtils.Clamp(connections.Max(c => c.Id) + 1, ushort.MaxValue / 2, ushort.MaxValue),
							                source.Device,
							                source.Control,
							                connector.Address,
							                switcher.Id,
							                switcher.SwitcherControl.Id,
							                MathUtils.Clamp(connections.Max(c => c.Destination.Address) + 1, ushort.MaxValue / 2,
							                                ushort.MaxValue),
							                connector.ConnectionType));
					}
				}

				Core.GetRoutingGraph().Connections.SetChildren(connections);

				// Set outputs on MockSource if applicable
				MockSourceDevice mockSource = sourceDevice as MockSourceDevice;
				if (mockSource != null)
				{
					mockSource.Controls.Clear();
					mockSource.AddSourceControl(source.Control);

					// MockRouteSourceControl pulls outputs from the routing graph
					//mockSource.Controls
					//          .GetControl<MockRouteSourceControl>(source.Endpoint.Control)
					//          .SetOutputs(message.SourceConnections[source.Id]);
				}
			}
		}

		private void AddDestinations(Message message, ShareDevicesData data, RemoteSwitcher switcher)
		{
			List<IDestination> newDestinations =
				data.Destinations.Where(destination => Core.GetRoutingGraph().Destinations.AddChild(destination)).ToList();
			foreach (IDestination destination in newDestinations)
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Informational, "Received {0} from Host {1}",
										 destination, message.From);
				destination.Remote = true;
			}

			foreach (IDestination destination in data.Destinations.Distinct())
			{
				// Get the device or create it if it doesn't exist
				IDevice destinationDevice = Core.Originators.ContainsChild(destination.Device)
					                            ? Core.Originators.GetChild<IDevice>(destination.Device)
					                            : null;

				if (destinationDevice == null)
				{
					MockDestinationDevice newDestinationDevice = new MockDestinationDevice
					{
						Id = destination.Device,
						Name = "Remote Destination Device"
					};
					newDestinationDevice.Controls.Clear();
					newDestinationDevice.AddDestinationControl(destination.Control);

					destinationDevice = newDestinationDevice;
					Core.Originators.AddChild(destinationDevice);
				}

				// Add connections to the remote switcher
				List<Connection> connections = Core.GetRoutingGraph().Connections.ToList();
				foreach (ConnectorInfo connector in data.DestinationConnections[destination.Id])
				{
					// Workaround for compiler warning
					IDestination destination1 = destination;
					ConnectorInfo connector1 = connector;

					if (!connections.Any(c => c.Destination.Device == destination1.Device &&
					                          c.Destination.Control == destination1.Control &&
					                          c.Destination.Address == connector1.Address &&
					                          c.ConnectionType == connector1.ConnectionType))
					{
						connections.Add(new Connection(
							                MathUtils.Clamp(connections.Max(c => c.Id) + 1, ushort.MaxValue / 2, ushort.MaxValue),
							                switcher.Id,
							                switcher.SwitcherControl.Id,
							                MathUtils.Clamp(connections.Max(c => c.Source.Address) + 1, ushort.MaxValue / 2, ushort.MaxValue),
							                destination.Device,
							                destination.Control,
							                connector.Address,
							                connector.ConnectionType));
					}
				}

				Core.GetRoutingGraph().Connections.SetChildren(connections);

				// Set inputs on MockDestination if applicable
				MockDestinationDevice mockDestination = destinationDevice as MockDestinationDevice;
				if (mockDestination != null)
				{
					mockDestination.Controls.Clear();
					mockDestination.AddDestinationControl(destination.Control);
					mockDestination.Controls
					               .GetControl<MockRouteDestinationControl>(destination.Control)
					               .SetInputs(data.DestinationConnections[destination.Id]);
				}
			}
		}
	}
}
