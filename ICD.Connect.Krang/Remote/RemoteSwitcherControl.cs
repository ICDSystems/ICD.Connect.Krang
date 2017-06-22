using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Services;
using ICD.Common.Utils;
using ICD.Connect.Krang.Core;
using ICD.Connect.Krang.Remote.Direct;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote
{
	public sealed class RemoteSwitcherControl : AbstractRouteSwitcherControl<RemoteSwitcher>
	{
		private readonly Core.Krang m_Krang;

		private readonly Dictionary<ConnectorInfo, int> m_RoutedOutputMap;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		public RemoteSwitcherControl(RemoteSwitcher parent)
			: base(parent, 0)
		{
			m_Krang = ServiceProvider.GetService<ICore>() as Core.Krang;
			m_RoutedOutputMap = new Dictionary<ConnectorInfo, int>();
		}

		#region IRouteSourceDevice Methods

		public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;

		public override bool GetActiveTransmissionState(int output, eConnectionType type)
		{
			ConnectorInfo key = new ConnectorInfo(output, type);
			return m_RoutedOutputMap.ContainsKey(key);
		}

		public override IEnumerable<ConnectorInfo> GetOutputs()
		{
			return m_Krang.RoutingGraph.Connections
			              .Where(c => c.Source.Device == Parent.Id && c.Source.Control == Id)
			              .Select(c => new ConnectorInfo(c.Source.Address, c.ConnectionType));
		}

		#endregion

		#region IRouteDestinationDevice Methods

		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;
		public override event EventHandler OnActiveInputsChanged;

		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			// TODO - needs better implementation
			return true;
		}

		public override bool GetInputActiveState(int input, eConnectionType type)
		{
			// TODO - needs better implementation
			return true;
		}

		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			return m_Krang.RoutingGraph.Connections
			              .Where(c => c.Destination.Device == Parent.Id && c.Destination.Control == Id)
			              .Select(c => new ConnectorInfo(c.Destination.Address, c.ConnectionType));
		}

		#endregion

		#region IRouteSwitcherDevice Methods

		public override event EventHandler<RouteChangeEventArgs> OnRouteChange;

		public override bool Route(RouteOperation info)
		{
			if (Parent.HasHostInfo && info.RouteRequestFrom != Parent.HostInfo)
			{
				DirectMessageManager dmManager = ServiceProvider.GetService<DirectMessageManager>();
				info.RouteRequestFrom = dmManager.GetHostInfo();
				m_Krang.RoutingGraph.PendingRouteStarted(info);
				dmManager.Send(Parent.HostInfo, new RouteDevicesMessage(info),
				               RouteFinished(info));
			}

			if (EnumUtils.HasMultipleFlags(info.ConnectionType))
			{
				foreach (eConnectionType type in EnumUtils.GetFlagsExceptNone(info.ConnectionType))
					m_RoutedOutputMap[new ConnectorInfo(info.LocalOutput, type)] = info.LocalInput;
			}
			return true;
		}

		private MessageResponseCallback<GenericMessage<bool>> RouteFinished(RouteOperation info)
		{
			return message => m_Krang.RoutingGraph.PendingRouteFinished(info, message.Value);
		}

		// change to clearroute
		// possibly go up the chain until switcher with input going to more than 1 output is found
		public override bool ClearOutput(int output, eConnectionType type)
		{
			// Send ClearOutput message
			m_RoutedOutputMap.Remove(new ConnectorInfo(output, type));
			return true;
		}

		// get route?
		public override IEnumerable<ConnectorInfo> GetInputs(int output, eConnectionType type)
		{
			return m_RoutedOutputMap.Where(kvp => kvp.Key.Address == output)
			                        .Select(val => GetInput(val.Value))
			                        .Distinct();
		}

		#endregion
	}
}
