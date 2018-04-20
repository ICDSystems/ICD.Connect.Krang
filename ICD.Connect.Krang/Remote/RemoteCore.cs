using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Json;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API;
using ICD.Connect.API.Info;
using ICD.Connect.API.Proxies;
using ICD.Connect.Krang.Remote.Direct.API;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;
using ICD.Connect.Settings.Proxies;
using Newtonsoft.Json;

namespace ICD.Connect.Krang.Remote
{
	/// <summary>
	/// RemoteCore simply represents a core instance that lives remotely.
	/// </summary>
	public sealed class RemoteCore : AbstractCore<RemoteCoreSettings>
	{
		private readonly Dictionary<int, IProxyOriginator> m_Proxies;
		private readonly Dictionary<IProxy, Func<ApiClassInfo, ApiClassInfo>> m_ProxyBuildCommand;

		private HostInfo m_Source;

		private DirectMessageManager DirectMessageManager { get { return ServiceProvider.GetService<DirectMessageManager>(); } }

		private RemoteApiResultHandler ApiResultHandler
		{
			get { return DirectMessageManager.GetMessageHandler<RemoteApiReply>() as RemoteApiResultHandler; }
		}

		private ICore Core { get { return ServiceProvider.GetService<ICore>(); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public RemoteCore()
		{
			m_Proxies = new Dictionary<int, IProxyOriginator>();
			m_ProxyBuildCommand = new Dictionary<IProxy, Func<ApiClassInfo, ApiClassInfo>>();

			Subscribe(ApiResultHandler);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			Unsubscribe(ApiResultHandler);

			base.DisposeFinal(disposing);

			DisposeProxies();
		}

		#region Methods

		/// <summary>
		/// Sets the host info for the remote API.
		/// </summary>
		/// <param name="source"></param>
		public void SetHostInfo(HostInfo source)
		{
			if (source == m_Source)
				return;

			DisposeProxies();

			m_Source = source;

			// Query the available devices
			QueryDevices();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sends the given command to the remote API.
		/// </summary>
		/// <param name="command"></param>
		private void SendCommand(ApiClassInfo command)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			IcdConsole.PrintLine("Sending command:");
			JsonUtils.Print(JsonConvert.SerializeObject(command));

			RemoteApiMessage message = new RemoteApiMessage { Command = command };
			DirectMessageManager.Send(m_Source, message);
		}

		/// <summary>
		/// Queries the remote API for the available devices.
		/// </summary>
		private void QueryDevices()
		{
			ApiClassInfo command =
				ApiCommandBuilder.NewCommand()
				                 .AtNode("ControlSystem")
				                 .AtNode("Core")
				                 .GetNodeGroup("Devices")
				                 .Complete();

			SendCommand(command);
		}

		/// <summary>
		/// Creates a proxy originator for the given class info if an originator with the given id does not exist.
		/// </summary>
		/// <param name="group"></param>
		/// <param name="id"></param>
		/// <param name="classInfo"></param>
		private IProxyOriginator LazyLoadProxyOriginator(string group, int id, ApiClassInfo classInfo)
		{
			if (m_Proxies.ContainsKey(id))
				return m_Proxies[id];

			if (Core.Originators.ContainsChild(id))
				return m_Proxies[id];

			Type proxyType = classInfo.GetProxyTypes().FirstOrDefault();
			if (proxyType == null)
				throw new InvalidOperationException(string.Format("No proxy type discovered for originator {0}", id));

			// Build the originator
			IProxyOriginator originator = ReflectionUtils.CreateInstance<IProxyOriginator>(proxyType);
			originator.Id = id;
			originator.Name = classInfo.Name;

			// Build the root command
			Func<ApiClassInfo, ApiClassInfo> buildCommand = local =>
				ApiCommandBuilder.NewCommand()
				                 .AtNode("ControlSystem")
				                 .AtNode("Core")
				                 .AtNodeGroup(group)
								 .AddKey((uint)id, local)
				                 .Complete();

			m_ProxyBuildCommand.Add(originator, buildCommand);
			m_Proxies.Add(id, originator);

			// Start handling the proxy callbacks
			Subscribe(originator);

			// Add to the core originator collection
			Core.Originators.AddChild(originator);

			// Initialize the proxy
			originator.Initialize();

			return originator;
		}

		/// <summary>
		/// Dispose all of the generated proxies.
		/// </summary>
		private void DisposeProxies()
		{
			foreach (IProxyOriginator proxy in m_Proxies.Values)
				DisposeProxy(proxy);

			m_Proxies.Clear();
		}

		/// <summary>
		/// Disposes the given proxy.
		/// </summary>
		/// <param name="proxy"></param>
		private void DisposeProxy(IProxy proxy)
		{
			if (proxy == null)
				return;

			Unsubscribe(proxy);

			m_ProxyBuildCommand.Remove(proxy);

			if (proxy is IDisposable)
				(proxy as IDisposable).Dispose();
		}

		#endregion

		#region Parse Response

		private void Subscribe(RemoteApiResultHandler results)
		{
			results.OnApiResult += ResultsOnOnApiResult;
		}

		private void Unsubscribe(RemoteApiResultHandler results)
		{
			results.OnApiResult -= ResultsOnOnApiResult;
		}

		private void ResultsOnOnApiResult(RemoteApiResultHandler sender, RemoteApiReply reply)
		{
			ParseResponse(reply);
		}

		/// <summary>
		/// Parses responses from the remote API.
		/// </summary>
		/// <param name="response"></param>
		private void ParseResponse(RemoteApiReply response)
		{
			if (response == null)
				throw new ArgumentNullException("response");

			IcdConsole.PrintLine("Received response:");
			JsonUtils.Print(JsonConvert.SerializeObject(response.Command));

			// A copy of the original command populated with results
			ApiClassInfo command = response.Command;

			ApiResult.ReadResultsRecursive(command, LogResults);

			try
			{
				// Parse the ControlSystem node
				ApiClassInfo controlSystemInfo;
				if (command.TryGetNodeContents("ControlSystem", out controlSystemInfo) && controlSystemInfo != null)
					ParseControlSystemResponse(controlSystemInfo);
			}
			catch (Exception e)
			{
				Logger.AddEntry(eSeverity.Error, e, "{0} failed to parse response - {1}", this, e.Message);
				throw;
			}
		}

		/// <summary>
		/// Parses the control system info for results.
		/// </summary>
		/// <param name="controlSystemInfo"></param>
		private void ParseControlSystemResponse(ApiClassInfo controlSystemInfo)
		{
			if (controlSystemInfo == null)
				throw new ArgumentNullException("controlSystemInfo");

			ApiClassInfo coreInfo;
			if (!controlSystemInfo.TryGetNodeContents("Core", out coreInfo))
				return;

			if (coreInfo != null)
				ParseCoreResponse(coreInfo);
		}

		/// <summary>
		/// Parses the core info for results.
		/// </summary>
		/// <param name="coreInfo"></param>
		private void ParseCoreResponse(ApiClassInfo coreInfo)
		{
			if (coreInfo == null)
				throw new ArgumentNullException("coreInfo");

			ApiNodeGroupInfo devicesGroupInfo;
			if (!coreInfo.TryGetNodeGroup("Devices", out devicesGroupInfo))
				return;

			if (devicesGroupInfo != null)
				ParseDevicesResponse(devicesGroupInfo);
		}

		/// <summary>
		/// Parses the devices group for results.
		/// </summary>
		/// <param name="devicesGroupInfo"></param>
		private void ParseDevicesResponse(ApiNodeGroupInfo devicesGroupInfo)
		{
			if (devicesGroupInfo == null)
				throw new ArgumentNullException("devicesGroupInfo");

			// Parse the devices result
			ApiResult result = devicesGroupInfo.Result;
			if (result != null)
				ParseDevicesGroupResult(result);

			foreach (ApiNodeGroupKeyInfo node in devicesGroupInfo)
				ParseDeviceResponse(node.Key, node.Node);
		}

		/// <summary>
		/// Parses the device info for results.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="deviceInfo"></param>
		private void ParseDeviceResponse(uint index, ApiClassInfo deviceInfo)
		{
			if (deviceInfo == null)
				throw new ArgumentNullException("deviceInfo");

			IProxyOriginator proxy = LazyLoadProxyOriginator("Devices", (int)index, deviceInfo);
			proxy.ParseInfo(deviceInfo);
		}

		#endregion

		#region Parse Results

		/// <summary>
		/// Parses the devices group result.
		/// </summary>
		/// <param name="result"></param>
		private void ParseDevicesGroupResult(ApiResult result)
		{
			if (result == null)
				throw new ArgumentNullException("devicesGroupInfo");

			ApiNodeGroupInfo devicesGroupInfo = result.Value as ApiNodeGroupInfo;
			if (devicesGroupInfo == null)
				return;

			foreach (ApiNodeGroupKeyInfo node in devicesGroupInfo.GetNodes())
			{
				// For testing
				int subsystemId = IdUtils.GetSubsystemId(IdUtils.SUBSYSTEM_DEVICES);
				int id = IdUtils.GetNewId(Core.Originators.GetChildrenIds(), subsystemId, 0);

				LazyLoadProxyOriginator("Devices", id, node.Node);
			}
		}

		/// <summary>
		/// Logs the given result to the logger service.
		/// </summary>
		/// <param name="result"></param>
		/// <param name="path"></param>
		private void LogResults(ApiResult result, Stack<IApiInfo> path)
		{
			if (result == null)
				throw new ArgumentNullException("result");

			eSeverity severity;
			string message = string.Format("{0}", result.Value);

			switch (result.ErrorCode)
			{
				case ApiResult.eErrorCode.Ok:
					severity = eSeverity.Debug;
					message = "Command OK.";
					break;

				case ApiResult.eErrorCode.MissingMember:
				case ApiResult.eErrorCode.MissingNode:
				case ApiResult.eErrorCode.InvalidParameter:
				case ApiResult.eErrorCode.Exception:
					severity = eSeverity.Error;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			string pathText = string.Join("/", path.Reverse().Select(i => i.Name).Where(n => n != null).ToArray());

			Logger.AddEntry(severity, "{0} - {1} - {2}", this, message, pathText);
		}

		#endregion

		#region Proxy Callbacks

		/// <summary>
		/// Subscribe to the proxy events.
		/// </summary>
		/// <param name="proxy"></param>
		private void Subscribe(IProxy proxy)
		{
			proxy.OnCommand += ProxyOnCommand;
		}

		/// <summary>
		/// Unsubscribe from the proxy events.
		/// </summary>
		/// <param name="proxy"></param>
		private void Unsubscribe(IProxy proxy)
		{
			proxy.OnCommand -= ProxyOnCommand;
		}

		/// <summary>
		/// Called when a proxy raises a command to be sent to the remote API.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ProxyOnCommand(object sender, ApiClassInfoEventArgs eventArgs)
		{
			IProxy proxy = sender as IProxy;
			if (proxy == null)
				return;

			// Build the full command from the API root to the proxy
			ApiClassInfo command = m_ProxyBuildCommand[proxy](eventArgs.Data);

			SendCommand(command);
		}

		#endregion
	}
}

