using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
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

namespace ICD.Connect.Krang.Remote
{
	/// <summary>
	/// RemoteCore simply represents a core instance that lives remotely.
	/// </summary>
	public sealed class RemoteCore : AbstractCore<RemoteCoreSettings>
	{
		private readonly CoreOriginatorCollection m_Originators;
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
			m_Originators = new CoreOriginatorCollection();
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

			DisposeOriginators();
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

			DisposeOriginators();

			m_Source = source;

			// Query the available devices
			QueryDevices();
		}

		#endregion

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
				                 .AtNodeGroup("Devices")
				                 .Complete();

			RemoteApiMessage message = new RemoteApiMessage {Command = command};

			DirectMessageManager.Send<RemoteApiReply>(m_Source, message, ParseResponse);
		}

		/// <summary>
		/// Parses responses from the remote API.
		/// </summary>
		/// <param name="response"></param>
		private void ParseResponse(RemoteApiReply response)
		{
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
			if (m_Originators.ContainsChild(id))
				return;

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

			m_Originators.AddChild(originator);

			// Start handling the proxy callbacks
			Subscribe(originator);

			// Add to the core originator collection
			Core.Originators.AddChild(originator);
		}

		/// <summary>
		/// Dispose all of the generated originators.
		/// </summary>
		private void DisposeOriginators()
		{
			foreach (IProxyOriginator originator in m_Originators.OfType<IProxyOriginator>())
				DisposeOriginator(originator);

			m_Originators.Clear();
		}

		/// <summary>
		/// Disposes the given originator.
		/// </summary>
		/// <param name="originator"></param>
		private void DisposeOriginator(IProxyOriginator originator)
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
			RemoteApiMessage message = new RemoteApiMessage {Command = command};

			DirectMessageManager.Send<RemoteApiReply>(m_Source, message, ParseResponse);
		}

		#endregion
	}
}

