using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;

namespace ICD.Connect.Krang.Routing
{
	/// <summary>
	/// Prints the routing graph in human-readable table form.
	/// </summary>
	public sealed class RoutingGraphTableBuilder
	{
		private readonly RoutingGraph m_RoutingGraph;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="routingGraph"></param>
		public RoutingGraphTableBuilder(RoutingGraph routingGraph)
		{
			m_RoutingGraph = routingGraph;
		}

		/// <summary>
		/// Returns the routing graph in table form.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			TableBuilder builder = new TableBuilder("Device", "Address", "Type", "Signal Detected", "Input Active",
			                                        "Output Active Transmission", "Connected Device");

			IRouteControl[] devices = m_RoutingGraph == null
				                          ? new IRouteControl[0]
				                          : m_RoutingGraph.Connections
				                                          .SelectMany(c => m_RoutingGraph.GetControls(c))
				                                          .Except((IRouteControl)null)
				                                          .Distinct()
				                                          .OrderBy(d => d.Id)
				                                          .ToArray();

			for (int index = 0; index < devices.Length; index++)
			{
				if (index != 0)
					builder.AddSeparator();

				string deviceName = devices[index].ToString();

				List<string[]> rows = GetDeviceRows(devices[index]);

				// Edge case - Sometimes we have mock devices with no inputs or outputs
				if (rows.Count == 0)
				{
					object[] row = new object[builder.ColumnsCount];
					row[0] = deviceName;
					builder.AddRow(row);
					continue;
				}

				foreach (string[] row in rows)
				{
					builder.AddRow(row.Prepend(deviceName).ToArray());
					deviceName = null;
				}
			}

			return builder.ToString();
		}

		/// <summary>
		/// Returns all rows for the device, for columns Address through Connected Device.
		/// </summary>
		/// <param name="device"></param>
		private List<string[]> GetDeviceRows(IRouteControl device)
		{
			List<string[]> output = new List<string[]>();

			IRouteSourceControl source = device as IRouteSourceControl;
			IRouteDestinationControl destination = device as IRouteDestinationControl;

			if (destination != null)
			{
				foreach (string[] row in destination.GetInputs().SelectMany(info => GetInputConnectorRows(destination, info)))
					output.Add(row);
			}

			if (source != null)
			{
				foreach (string[] row in source.GetOutputs().SelectMany(info => GetOutputConnectorRows(source, info)))
					output.Add(row);
			}

			return output;
		}

		private IEnumerable<string[]> GetInputConnectorRows(IRouteDestinationControl destination, ConnectorInfo info)
		{
			List<string[]> output = new List<string[]>();

			string address = string.Format("Input {0}", info.Address);

			foreach (eConnectionType flag in EnumUtils.GetFlagsExceptNone(info.ConnectionType))
			{
				bool inputDetected = destination.GetSignalDetectedState(info.Address, flag);
				bool inputActive = destination.GetInputActiveState(info.Address, flag);

				int outputAddress;
				IRouteSourceControl source = m_RoutingGraph.GetSourceControl(destination, info.Address, flag, out outputAddress);
				string connectedDevice = source == null ? null : string.Format("{0} - Output {1}", source, outputAddress);

				string[] row =
				{
					address,
					flag.ToString(),
					inputDetected.ToString(),
					inputActive.ToString(),
					null,
					connectedDevice
				};

				output.Add(row);

				address = null;
			}

			return output;
		}

		private IEnumerable<string[]> GetOutputConnectorRows(IRouteSourceControl source, ConnectorInfo info)
		{
			List<string[]> output = new List<string[]>();

			string address = string.Format("Output {0}", info.Address);

			foreach (eConnectionType flag in EnumUtils.GetFlagsExceptNone(info.ConnectionType))
			{
				string outputActiveString = GetOutputActiveString(source, info, flag);

				int input;
				IRouteDestinationControl destination = m_RoutingGraph.GetDestinationControl(source, info.Address, flag, out input);
				string connectedDevice = destination == null ? null : string.Format("{0} - Input {1}", destination, input);

				string[] row =
				{
					address,
					flag.ToString(),
					null,
					null,
					outputActiveString,
					connectedDevice
				};

				output.Add(row);

				address = null;
			}

			return output;
		}

		/// <summary>
		/// Builds a string for the active state of the given connector.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="info"></param>
		/// <param name="flag"></param>
		/// <returns></returns>
		private static string GetOutputActiveString(IRouteSourceControl source, ConnectorInfo info, eConnectionType flag)
		{
			bool outputActive = source.GetActiveTransmissionState(info.Address, flag);
			IRouteMidpointControl switcher = source as IRouteMidpointControl;

			if (switcher == null)
				return outputActive.ToString();

			ConnectorInfo? input = switcher.GetInput(info.Address, flag);

			return string.Format("{0} (input={1})", outputActive, input == null ? null : (object)input.Value.Address);
		}
	}
}
