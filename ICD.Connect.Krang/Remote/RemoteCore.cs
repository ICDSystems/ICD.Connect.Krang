using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Json;
using ICD.Common.Utils.Services;
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
		private readonly IcdHashSet<IProxy> m_Proxies;
		private readonly Dictionary<IProxy, Func<ApiClassInfo, ApiClassInfo>> m_ProxyBuildCommand;
		private readonly Dictionary<int, int> m_TempProxyIds;

		private HostInfo m_Source;

		private DirectMessageManager DirectMessageManager { get { return ServiceProvider.GetService<DirectMessageManager>(); } }

		private RemoteApiResultHandler ApiResultHandler { get { return ServiceProvider.GetService<RemoteApiResultHandler>(); } }

		private ICore Core { get { return ServiceProvider.GetService<ICore>(); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public RemoteCore()
		{
			m_Proxies = new IcdHashSet<IProxy>();
			m_ProxyBuildCommand = new Dictionary<IProxy, Func<ApiClassInfo, ApiClassInfo>>();
			m_TempProxyIds = new Dictionary<int, int>();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
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

		private void SendCommand(ApiClassInfo command)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			IcdConsole.PrintLine("Sending command:");
			JsonUtils.Print(JsonConvert.SerializeObject(command));

			RemoteApiMessage message = new RemoteApiMessage { Command = command };
			DirectMessageManager.Send<RemoteApiReply>(m_Source, message, ParseResponse);
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
				                 .AtNodeGroup("Devices")
				                 .Complete();

			SendCommand(command);
		}

		/// <summary>
		/// Parses responses from the remote API.
		/// </summary>
		/// <param name="response"></param>
		private void ParseResponse(RemoteApiReply response)
		{
			IcdConsole.PrintLine("Received response:");
			JsonUtils.Print(JsonConvert.SerializeObject(response.Command));

			ApiHandler.ReadResultsRecursive(response.Command, ParseResult);
		}

		/// <summary>
		/// Parses an individual result from a response.
		/// </summary>
		/// <param name="result"></param>
		private void ParseResult(ApiResult result)
		{
			ApiNodeGroupInfo nodeGroup = result.Value as ApiNodeGroupInfo;
			if (nodeGroup == null || nodeGroup.Name != "Devices")
				return;

			foreach (KeyValuePair<uint, ApiClassInfo> kvp in nodeGroup.GetNodes())
			{
				// For testing
				int subsystemId = IdUtils.GetSubsystemId(IdUtils.SUBSYSTEM_DEVICES);
				int id = IdUtils.GetNewId(Core.Originators.GetChildrenIds(), subsystemId, 0);
				m_TempProxyIds[id] = (int)kvp.Key;

				LazyLoadProxyOriginator("Devices", id, kvp.Value);
			}
		}

		/// <summary>
		/// Creates a proxy originator for the given class info if an originator with the given id does not exist.
		/// </summary>
		/// <param name="group"></param>
		/// <param name="id"></param>
		/// <param name="classInfo"></param>
		private void LazyLoadProxyOriginator(string group, int id, ApiClassInfo classInfo)
		{
			if (Core.Originators.ContainsChild(id))
				return;

			Type proxyType = classInfo.GetProxyTypes().FirstOrDefault();
			if (proxyType == null)
				return;

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
								 .AddKey((uint)m_TempProxyIds[id], local)
				                 .Complete();

			m_ProxyBuildCommand.Add(originator, buildCommand);

			m_Proxies.Add(originator);

			// Start handling the proxy callbacks
			Subscribe(originator);

			// Add to the core originator collection
			Core.Originators.AddChild(originator);
		}

		/// <summary>
		/// Dispose all of the generated proxies.
		/// </summary>
		private void DisposeProxies()
		{
			foreach (IProxy proxy in m_Proxies)
				DisposeProxy(proxy);

			m_Proxies.Clear();
		}

		/// <summary>
		/// Disposes the given proxy.
		/// </summary>
		/// <param name="originator"></param>
		private void DisposeProxy(IProxy originator)
		{
			if (originator == null)
				return;

			Unsubscribe(originator);

			m_ProxyBuildCommand.Remove(originator);

			if (originator is IDisposable)
				(originator as IDisposable).Dispose();
		}

		#endregion

		#region Proxy Callbacks

		/// <summary>
		/// Subscribe to the proxy events.
		/// </summary>
		/// <param name="originator"></param>
		private void Subscribe(IProxy originator)
		{
			originator.OnCommand += ProxyOnCommand;
		}

		/// <summary>
		/// Unsubscribe from the proxy events.
		/// </summary>
		/// <param name="originator"></param>
		private void Unsubscribe(IProxy originator)
		{
			originator.OnCommand -= ProxyOnCommand;
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

