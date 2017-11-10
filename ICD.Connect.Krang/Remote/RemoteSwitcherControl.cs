using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Services;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Krang.Core;
using ICD.Connect.Krang.Remote.Direct;
using ICD.Connect.Krang.Routing;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.Utils;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote
{
	public sealed class RemoteSwitcherControl : AbstractRouteSwitcherControl<RemoteSwitcher>
	{
		public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;
		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;
		public override event EventHandler<ActiveInputStateChangeEventArgs> OnActiveInputsChanged;
		public override event EventHandler<RouteChangeEventArgs> OnRouteChange;

		private readonly KrangCore m_Krang;

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

		public override IEnumerable<ConnectorInfo> GetOutputs()
		{
			IRoutingGraph graph = m_Krang.RoutingGraph;
			if (graph == null)
				return Enumerable.Empty<ConnectorInfo>();

			return graph.Connections
			              .Where(c => c.Source.Device == Parent.Id && c.Source.Control == Id)
			              .Select(c => new ConnectorInfo(c.Source.Address, c.ConnectionType));
		}

		#endregion

		#region IRouteDestinationDevice Methods

		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			return m_Cache.GetSourceDetectedState(input, type);
		}

		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			IRoutingGraph graph = m_Krang.RoutingGraph;
			if (graph == null)
				return Enumerable.Empty<ConnectorInfo>();

			return graph.Connections
			              .Where(c => c.Destination.Device == Parent.Id && c.Destination.Control == Id)
			              .Select(c => new ConnectorInfo(c.Destination.Address, c.ConnectionType));
		}

		#endregion

		#region IRouteSwitcherDevice Methods

		public override bool Route(RouteOperation info)
		{
			if (Parent.HasHostInfo && info.RouteRequestFrom != Parent.HostInfo)
			{
				DirectMessageManager dmManager = ServiceProvider.GetService<DirectMessageManager>();
				info.RouteRequestFrom = dmManager.GetHostInfo();

				RoutingGraph graph = m_Krang.RoutingGraph;
				if (graph != null)
					graph.PendingRouteStarted(info);

				dmManager.Send(Parent.HostInfo, new RouteDevicesMessage(info),
				               RouteFinished(info));
			}

			return m_Cache.SetInputForOutput(info.LocalOutput, info.LocalInput, info.ConnectionType);
		}

		private MessageResponseCallback<GenericMessage<bool>> RouteFinished(RouteOperation info)
		{
			return message =>
			       {
				       RoutingGraph graph = m_Krang.RoutingGraph;
				       if (graph != null)
					       graph.PendingRouteFinished(info, message.Value);
			       };
		}

		// change to clearroute
		// possibly go up the chain until switcher with input going to more than 1 output is found
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
