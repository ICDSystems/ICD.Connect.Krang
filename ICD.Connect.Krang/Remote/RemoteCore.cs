using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Info;
using ICD.Connect.API.Nodes;
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
	public sealed class RemoteCore : IDisposable, IConsoleNode
	{
		private const long MESSAGE_TIMEOUT = 60 * 1000;

		private readonly Dictionary<IProxy, Func<ApiClassInfo, ApiClassInfo>> m_ProxyBuildCommand;
		private readonly SafeCriticalSection m_CriticalSection;

		private readonly DirectMessageManager m_DirectMessageManager;
		private readonly RemoteApiCommandHandler m_ApiResultHandler;
		private readonly ICore m_LocalCore;
		private readonly HostSessionInfo m_RemoteHost;

		private readonly ILoggingContext m_Logger;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="localCore"></param>
		/// <param name="remoteHost"></param>
		public RemoteCore(ICore localCore, HostSessionInfo remoteHost)
		{
			m_Logger = new ServiceLoggingContext(this);

			m_ProxyBuildCommand = new Dictionary<IProxy, Func<ApiClassInfo, ApiClassInfo>>();
			m_CriticalSection = new SafeCriticalSection();

			m_LocalCore = localCore;
			m_RemoteHost = remoteHost;

			m_DirectMessageManager = ServiceProvider.GetService<DirectMessageManager>();
			m_ApiResultHandler = m_DirectMessageManager.GetMessageHandler<ApiMessageData>() as RemoteApiCommandHandler;
		
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
				foreach (IProxy proxy in m_ProxyBuildCommand.Keys.ToArray())
					DeinitializeProxyOriginator(proxy);
			}
			finally
			{
				m_CriticalSection.Leave();
			}
		}

		public override string ToString()
		{
			return new ReprBuilder(this).AppendProperty("Host", m_RemoteHost).ToString();
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

			ApiMessageData data = new ApiMessageData { Command = command };
			m_DirectMessageManager.Send(m_RemoteHost, Message.FromData(data), HandleMessageReply, HandleMessageTimeout, MESSAGE_TIMEOUT);
		}

		/// <summary>
		/// Called when a message gets a reply.
		/// </summary>
		/// <param name="reply"></param>
		private void HandleMessageReply(Message reply)
		{
			if (reply.From != m_RemoteHost)
				return;

			ApiMessageData data = reply.Data as ApiMessageData;
			if (data != null)
				ParseResponse(data);
		}

		/// <summary>
		/// Called when a message times out.
		/// </summary>
		/// <param name="message"></param>
		private void HandleMessageTimeout(Message message)
		{
			//IcdConsole.PrintLine(eConsoleColor.Magenta, JsonUtils.Format(message));
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
			{
				IcdConsole.PrintLine(eConsoleColor.Blue, "InitializeProxy: No originator at id {0}", id);
				return null;
			}

			IProxyOriginator proxyOriginator = originator as IProxyOriginator;
			if (proxyOriginator == null)
			{
				IcdConsole.PrintLine(eConsoleColor.Blue, "InitializeProxy: Originator at id {0} is a not a proxy", id);
				return null;
			}

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
			}
			finally
			{
				m_CriticalSection.Leave();
			}

			// Initialize the proxy
			IcdConsole.PrintLine(eConsoleColor.Blue, "InitializeProxy: Inatilizing proxy {0}", id);
			proxyOriginator.Initialize();

			return proxyOriginator;
		}

		/// <summary>
		/// Removes from the collection and deinitializes the proxy.
		/// </summary>
		/// <param name="proxy"></param>
		private void DeinitializeProxyOriginator(IProxy proxy)
		{
			if (proxy == null)
				throw new ArgumentNullException("proxy");

			if (m_CriticalSection.Execute(() => !m_ProxyBuildCommand.Remove(proxy)))
				return;

			IcdConsole.PrintLine(eConsoleColor.Blue, "DeinitializeProxy: Deinitializing {0}", proxy);

			Unsubscribe(proxy);
			proxy.Deinitialize();
		}

		#endregion

		#region API Result Callbacks

		/// <summary>
		/// Subscribe to the result handler events.
		/// </summary>
		/// <param name="apiResultHandler"></param>
		private void Subscribe(RemoteApiCommandHandler apiResultHandler)
		{
			apiResultHandler.OnAsyncApiResult += ApiResultHandlerOnAsyncApiResult;
		}

		/// <summary>
		/// Unsubscribe from the result handler.
		/// </summary>
		/// <param name="apiResultHandler"></param>
		private void Unsubscribe(RemoteApiCommandHandler apiResultHandler)
		{
			apiResultHandler.OnAsyncApiResult -= ApiResultHandlerOnAsyncApiResult;
		}

		/// <summary>
		/// Called when we receive a result from the API handler.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="reply"></param>
		private void ApiResultHandlerOnAsyncApiResult(RemoteApiCommandHandler sender, Message reply)
		{
			HandleMessageReply(reply);
		}

		/// <summary>
		/// Parses responses from the remote API.
		/// </summary>
		/// <param name="response"></param>
		private void ParseResponse(ApiMessageData response)
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
				m_Logger.Log(eSeverity.Error, e, "Failed to parse response - {0}", e.Message);
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

			m_LocalCore.Logger.Log(severity, "{0} - {1} - {2}", this, message, pathText);
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
					continue;

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
			// TODO - We shouldn't be getting here
			if (deviceInfo == null)
				return;
				//throw new ArgumentNullException("deviceInfo");

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
			Func<ApiClassInfo, ApiClassInfo> buildCommand = m_CriticalSection.Execute(() => m_ProxyBuildCommand[proxy]);
			ApiClassInfo command = buildCommand(eventArgs.Data);

			SendCommand(command);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return m_RemoteHost.ToString(); } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Represents a remote core"; } }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("GUID", m_RemoteHost.Session);
			addRow("Host", m_RemoteHost.Host);
			addRow("Proxies", m_CriticalSection.Execute(() => m_ProxyBuildCommand.Count));
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield break;
		}

		#endregion
	}
}

