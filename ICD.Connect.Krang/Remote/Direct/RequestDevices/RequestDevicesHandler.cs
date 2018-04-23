using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Services;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Krang.Remote.Direct.ShareDevices;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Endpoints.Groups;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Routing.Extensions;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote.Direct.RequestDevices
{
	public sealed class RequestDevicesHandler : AbstractMessageHandler<RequestDevicesMessage, IReply>
	{
		protected override IReply HandleMessage(RequestDevicesMessage message)
		{
			ICore core = ServiceProvider.GetService<ICore>();
			List<ISource> sources = core.GetRoutingGraph().Sources.Where(s => message.Sources.Contains(s.Id)).ToList();
			List<IDestination> destinations =
				core.GetRoutingGraph().Destinations.Where(d => message.Destinations.Contains(d.Id)).ToList();
			List<IDestinationGroup> destinationGroups =
				core.GetRoutingGraph().DestinationGroups.Where(d => message.DestinationGroups.Contains(d.Id)).ToList();

			// TODO - Does this work properly for switchers or throughput devices?
			Dictionary<int, IEnumerable<ConnectorInfo>> sourceConnections = new Dictionary<int, IEnumerable<ConnectorInfo>>();
			Dictionary<int, IEnumerable<ConnectorInfo>> destinationConnections =
				new Dictionary<int, IEnumerable<ConnectorInfo>>();

			foreach (ISource source in sources)
			{
				IRouteSourceControl control =
					core.Originators.GetChild<IDeviceBase>(source.Device)
					    .Controls.GetControl<IRouteSourceControl>(source.Control);

				if (!sourceConnections.ContainsKey(source.Id))
					sourceConnections.Add(source.Id, control.GetOutputs());
			}

			foreach (IDestination destination in destinations)
			{
				DeviceControlInfo info = new DeviceControlInfo(destination.Device, destination.Control);
				IRouteDestinationControl control =
					core.Originators.GetChild<IDeviceBase>(destination.Device)
					    .Controls.GetControl<IRouteDestinationControl>(destination.Control);

				if (!destinationConnections.ContainsKey(destination.Id))
					destinationConnections.Add(destination.Id, control.GetInputs());
			}

			ServiceProvider.GetService<DirectMessageManager>().Send(message.MessageFrom, new ShareDevicesMessage
			{
				Sources = sources,
				Destinations = destinations,
				SourceConnections = sourceConnections,
				DestinationConnections = destinationConnections,
				DestinationGroups = destinationGroups
			});

			return null;
		}
	}
}
