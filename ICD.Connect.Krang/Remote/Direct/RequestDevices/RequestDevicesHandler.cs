using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Services;
using ICD.Connect.Devices;
using ICD.Connect.Krang.Remote.Direct.ShareDevices;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Routing.Extensions;
using ICD.Connect.Settings.Cores;

namespace ICD.Connect.Krang.Remote.Direct.RequestDevices
{
	public sealed class RequestDevicesHandler : AbstractMessageHandler
	{
		/// <summary>
		/// Gets the message type that this handler is expecting.
		/// </summary>
		public override Type MessageType { get { return typeof(RequestDevicesData); } }

		public override Message HandleMessage(Message message)
		{
			RequestDevicesData data = message.Data as RequestDevicesData;
			if (data == null)
				return null;

			ICore core = ServiceProvider.GetService<ICore>();
			List<ISource> sources = core.GetRoutingGraph().Sources.Where(s => data.Sources.Contains(s.Id)).ToList();
			List<IDestination> destinations =
				core.GetRoutingGraph().Destinations.Where(d => data.Destinations.Contains(d.Id)).ToList();

			// TODO - Does this work properly for switchers or throughput devices?
			Dictionary<int, IEnumerable<ConnectorInfo>> sourceConnections = new Dictionary<int, IEnumerable<ConnectorInfo>>();
			Dictionary<int, IEnumerable<ConnectorInfo>> destinationConnections =
				new Dictionary<int, IEnumerable<ConnectorInfo>>();

			foreach (ISource source in sources)
			{
				IRouteSourceControl control =
					core.Originators
					    .GetChild<IDevice>(source.Device)
					    .Controls
					    .GetControl<IRouteSourceControl>(source.Control);

				if (!sourceConnections.ContainsKey(source.Id))
					sourceConnections.Add(source.Id, control.GetOutputs());
			}

			foreach (IDestination destination in destinations)
			{
				IRouteDestinationControl control =
					core.Originators
					    .GetChild<IDevice>(destination.Device)
					    .Controls
					    .GetControl<IRouteDestinationControl>(destination.Control);

				if (!destinationConnections.ContainsKey(destination.Id))
					destinationConnections.Add(destination.Id, control.GetInputs());
			}

			ShareDevicesData response = new ShareDevicesData
			{
				Sources = sources,
				Destinations = destinations,
				SourceConnections = sourceConnections,
				DestinationConnections = destinationConnections
			};

			return Message.FromData(response);
		}
	}
}
