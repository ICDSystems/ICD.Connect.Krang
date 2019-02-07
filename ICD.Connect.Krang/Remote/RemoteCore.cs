using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API;
using ICD.Connect.API.Info;
using ICD.Connect.API.Proxies;
using ICD.Connect.Krang.Remote.Direct.API;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Cores;
using ICD.Connect.Settings.Originators;
using ICD.Connect.Settings.Proxies;

namespace ICD.Connect.Krang.Remote
{
	/// <summary>
	/// RemoteCore simply represents a core instance that lives remotely.
	/// </summary>
	public sealed class RemoteCore : IDisposable
	{
		private readonly Dictionary<IProxy, Func<ApiClassInfo, ApiClassInfo>> m_ProxyBuildCommand;
		private readonly SafeCriticalSection m_CriticalSection;

		private readonly DirectMessageManager m_DirectMessageManager;
		private readonly RemoteApiResultHandler m_ApiResultHandler;
		private readonly ICore m_LocalCore;
		private readonly HostInfo m_RemoteHost;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="localCore"></param>
		/// <param name="remoteHost"></param>
		public RemoteCore(ICore localCore, HostInfo remoteHost)
		{
			m_ProxyBuildCommand = new Dictionary<IProxy, Func<ApiClassInfo, ApiClassInfo>>();
			m_CriticalSection = new SafeCriticalSection();

			m_LocalCore = localCore;
			m_RemoteHost = remoteHost;

			m_DirectMessageManager = ServiceProvider.GetService<DirectMessageManager>();
			m_ApiResultHandler = m_DirectMessageManager.GetMessageHandler<RemoteApiReply>() as RemoteApiResultHandler;
		
			Subscribe(m_ApiResultHandler);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Unsubscribe(m_ApiResultHandler);

			m_CriticalSection.Enter();

			try
			{
				foreach (IProxy proxy in m_ProxyBuildCommand.Keys)
					Unsubscribe(proxy);
				m_ProxyBuildCommand.Clear();
			}
			finally
			{
				m_CriticalSection.Leave();
			}
		}

		/// <summary>
		/// Queries the remote core for known originators.
		/// </summary>
		public void Initialize()
		{
			// TODO - Query all originators, not just devices
			QueryDevices();
		}

		#region Private Methods

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
		/// Sends the given command to the remote API.
		/// </summary>
		/// <param name="command"></param>
		private void SendCommand(ApiClassInfo command)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			RemoteApiMessage message = new RemoteApiMessage { Command = command };
			m_DirectMessageManager.Send(m_RemoteHost, message);
		}

		/// <summary>
		/// Gets the proxy originator with the given id.
		/// Subscribes and initializes if this is the first time accessing the originator.
		/// </summary>
		/// <param name="group"></param>
		/// <param name="id"></param>
		[CanBeNull]
		private IProxyOriginator InitializeProxyOriginator(string group, int id)
		{
			IOriginator originator;
			if (!m_LocalCore.Originators.TryGetChild(id, out originator))
				return null;

			IProxyOriginator proxyOriginator = originator as IProxyOriginator;
			if (proxyOriginator == null)
				return null;

			m_CriticalSection.Enter();

			try
			{
				if (m_ProxyBuildCommand.ContainsKey(proxyOriginator))
					return proxyOriginator;

				// Build the root command
				Func<ApiClassInfo, ApiClassInfo> buildCommand = local =>
					ApiCommandBuilder.NewCommand()
									 .AtNode("ControlSystem")
									 .AtNode("Core")
									 .AtNodeGroup(group)
									 .AddKey((uint)id, local)
									 .Complete();

				m_ProxyBuildCommand.Add(proxyOriginator, buildCommand);

				// Start handling the proxy callbacks
				Subscribe(proxyOriginator);

				// Initialize the proxy
				proxyOriginator.Initialize();

				return proxyOriginator;
			}
			finally
			{
				m_CriticalSection.Leave();
			}
		}

		#endregion

		#region API Result Callbacks

		/// <summary>
		/// Subscribe to the result handler events.
		/// </summary>
		/// <param name="apiResultHandler"></param>
		private void Subscribe(RemoteApiResultHandler apiResultHandler)
		{
			apiResultHandler.OnApiResult += ApiResultHandlerOnApiResult;
		}

		/// <summary>
		/// Unsubscribe from the result handler.
		/// </summary>
		/// <param name="apiResultHandler"></param>
		private void Unsubscribe(RemoteApiResultHandler apiResultHandler)
		{
			apiResultHandler.OnApiResult -= ApiResultHandlerOnApiResult;
		}

		/// <summary>
		/// Called when we receive a result from the API handler.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="reply"></param>
		private void ApiResultHandlerOnApiResult(RemoteApiResultHandler sender, RemoteApiReply reply)
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
				m_LocalCore.Logger.AddEntry(eSeverity.Error, e, "{0} failed to parse response - {1}", this, e.Message);
				throw;
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

			m_LocalCore.Logger.AddEntry(severity, "{0} - {1} - {2}", this, message, pathText);
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
		/// Parses the devices group result.
		/// </summary>
		/// <param name="result"></param>
		private void ParseDevicesGroupResult(ApiResult result)
		{
			if (result == null)
				throw new ArgumentNullException("result");

			ApiNodeGroupInfo devicesGroupInfo = result.Value as ApiNodeGroupInfo;
			if (devicesGroupInfo == null)
				return;

			foreach (ApiNodeGroupKeyInfo node in devicesGroupInfo.GetNodes())
			{
				// Don't create proxy around existing proxies
				if (node.Node.IsProxy)
					return;

				InitializeProxyOriginator("Devices", (int)node.Key);
			}
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

			// Don't create proxy around existing proxies
			if (deviceInfo.IsProxy)
				return;

			IProxyOriginator proxy = InitializeProxyOriginator("Devices", (int)index);
			if (proxy != null)
				proxy.ParseInfo(deviceInfo);
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

