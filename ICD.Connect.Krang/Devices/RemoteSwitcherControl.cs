using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Connect.Krang.Core;
using ICD.Connect.Krang.Remote.Direct.RouteDevices;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Routing.Utils;
using ICD.Connect.Settings.Cores;

namespace ICD.Connect.Krang.Devices
{
	public sealed class RemoteSwitcherControl : AbstractRouteSwitcherControl<RemoteSwitcher>
	{
		/// <summary>
		/// Raised when the device starts/stops actively transmitting on an output.
		/// </summary>
		public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;

		/// <summary>
		/// Raised when an input source status changes.
		/// </summary>
		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;

		/// <summary>
		/// Raised when the device starts/stops actively using an input, e.g. unroutes an input.
		/// </summary>
		public override event EventHandler<ActiveInputStateChangeEventArgs> OnActiveInputsChanged;

		/// <summary>
		/// Called when a route changes.
		/// </summary>
		public override event EventHandler<RouteChangeEventArgs> OnRouteChange;

		private readonly KrangCore m_Krang;

		private RoutingGraph m_CachedRoutingGraph;

		/// <summary>
		/// Gets the routing graph.
		/// </summary>
		public RoutingGraph RoutingGraph
		{
			get { return m_CachedRoutingGraph = m_CachedRoutingGraph ?? m_Krang.RoutingGraph; }
		}

		/// <summary>
		/// Maps outputs to inputs.
		/// </summary>
		private readonly SwitcherCache m_Cache;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		public RemoteSwitcherControl(RemoteSwitcher parent)
			: base(parent, 0)
		{
			m_Krang = ServiceProvider.GetService<ICore>() as KrangCore;
			m_Cache = new SwitcherCache();

			Subscribe(m_Cache);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnActiveTransmissionStateChanged = null;
			OnSourceDetectionStateChange = null;
			OnActiveInputsChanged = null;
			OnRouteChange = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_Cache);
		}

		#region IRouteSourceDevice Methods

		/// <summary>
		/// Gets the output at the given address.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public override ConnectorInfo GetOutput(int address)
		{
			Connection connection = RoutingGraph.Connections.GetOutputConnection(this, address);
			if (connection == null)
				throw new ArgumentOutOfRangeException("address");

			return new ConnectorInfo(address, connection.ConnectionType);
		}

		/// <summary>
		/// Returns true if the source contains an output at the given address.
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public override bool ContainsOutput(int output)
		{
			return RoutingGraph.Connections.GetOutputConnection(this, output) != null;
		}

		/// <summary>
		/// Returns the outputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetOutputs()
		{
			return RoutingGraph.Connections
			                   .GetOutputConnections(Parent.Id, Id)
			                   .Select(c => new ConnectorInfo(c.Source.Address, c.ConnectionType));
		}

		#endregion

		#region IRouteDestinationDevice Methods

		/// <summary>
		/// Returns true if a signal is detected at the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			return m_Cache.GetSourceDetectedState(input, type);
		}

		/// <summary>
		/// Gets the input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override ConnectorInfo GetInput(int input)
		{
			Connection connection = RoutingGraph.Connections.GetInputConnection(this, input);
			if (connection == null)
				throw new ArgumentOutOfRangeException("address");

			return new ConnectorInfo(input, connection.ConnectionType);
		}

		/// <summary>
		/// Returns true if the destination contains an input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override bool ContainsInput(int input)
		{
			return RoutingGraph.Connections.GetInputConnection(this, input) != null;
		}

		/// <summary>
		/// Returns the inputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			return RoutingGraph.Connections
			                   .GetInputConnections(Parent.Id, Id)
			                   .Select(c => new ConnectorInfo(c.Destination.Address, c.ConnectionType));
		}

		#endregion

		#region IRouteSwitcherDevice Methods

		/// <summary>
		/// Performs the given route operation.
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		public override bool Route(RouteOperation info)
		{
			if (Parent.HasHostInfo && info.RouteRequestFrom != Parent.HostInfo)
			{
				DirectMessageManager dmManager = ServiceProvider.GetService<DirectMessageManager>();
				info.RouteRequestFrom = dmManager.GetHostInfo();

				RoutingGraph.PendingRouteStarted(info);

				dmManager.Send<RouteDevicesReply>(Parent.HostInfo, new RouteDevicesMessage(info), r => RouteFinished(info, r));
			}

			return m_Cache.SetInputForOutput(info.LocalOutput, info.LocalInput, info.ConnectionType);
		}

		private void RouteFinished(RouteOperation info, RouteDevicesReply response)
		{
			RoutingGraph.PendingRouteFinished(info, response.Result);
		}

		/// <summary>
		/// Stops routing to the given output.
		/// 
		/// change to clearroute
		/// possibly go up the chain until switcher with input going to more than 1 output is found
		/// </summary>
		/// <param name="output"></param>
		/// <param name="type"></param>
		/// <returns>True if successfully cleared.</returns>
		public override bool ClearOutput(int output, eConnectionType type)
		{
			// todo - Send ClearOutput message
			return m_Cache.SetInputForOutput(output, null, type);
		}

		/// <summary>
		/// Gets the input routed to the given output matching the given type.
		/// </summary>
		/// <param name="output"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">Type has multiple flags.</exception>
		public override ConnectorInfo? GetInput(int output, eConnectionType type)
		{
			if (EnumUtils.HasMultipleFlags(type))
				throw new InvalidOperationException("Type has multiple flags");

			return m_Cache.GetInputConnectorInfoForOutput(output, type);
		}

		/// <summary>
		/// Gets the outputs for the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetOutputs(int input, eConnectionType type)
		{
			return m_Cache.GetOutputsForInput(input, type);
		}

		#endregion

		#region Cache Callbacks

		/// <summary>
		/// Subscribe to the cache events.
		/// </summary>
		/// <param name="cache"></param>
		private void Subscribe(SwitcherCache cache)
		{
			cache.OnActiveInputsChanged += CacheOnActiveInputsChanged;
			cache.OnActiveTransmissionStateChanged += CacheOnActiveTransmissionStateChanged;
			cache.OnRouteChange += CacheOnRouteChange;
			cache.OnSourceDetectionStateChange += CacheOnSourceDetectionStateChange;
		}

		/// <summary>
		/// Unsubscribe from the cache events.
		/// </summary>
		/// <param name="cache"></param>
		private void Unsubscribe(SwitcherCache cache)
		{
			cache.OnActiveInputsChanged -= CacheOnActiveInputsChanged;
			cache.OnActiveTransmissionStateChanged -= CacheOnActiveTransmissionStateChanged;
			cache.OnRouteChange -= CacheOnRouteChange;
			cache.OnSourceDetectionStateChange -= CacheOnSourceDetectionStateChange;
		}

		private void CacheOnSourceDetectionStateChange(object sender, SourceDetectionStateChangeEventArgs args)
		{
			OnSourceDetectionStateChange.Raise(this, new SourceDetectionStateChangeEventArgs(args));
		}

		private void CacheOnRouteChange(object sender, RouteChangeEventArgs args)
		{
			OnRouteChange.Raise(this, new RouteChangeEventArgs(args));
		}

		private void CacheOnActiveTransmissionStateChanged(object sender, TransmissionStateEventArgs args)
		{
			OnActiveTransmissionStateChanged.Raise(this, new TransmissionStateEventArgs(args));
		}

		private void CacheOnActiveInputsChanged(object sender, ActiveInputStateChangeEventArgs args)
		{
			OnActiveInputsChanged.Raise(this, new ActiveInputStateChangeEventArgs(args));
		}

		#endregion
	}
}
