using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Connect.API;
using ICD.Connect.API.Info;
using ICD.Connect.Krang.Remote.Direct.API;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote
{
	/// <summary>
	/// RemoteCore simply represents a core instance that lives remotely.
	/// </summary>
	public sealed class RemoteCore : AbstractCore<RemoteCoreSettings>
	{
		private readonly CoreOriginatorCollection m_Originators;

		private DirectMessageManager DirectMessageManager { get { return ServiceProvider.GetService<DirectMessageManager>(); } }

		private RemoteApiResultHandler ApiResultHandler { get { return ServiceProvider.GetService<RemoteApiResultHandler>(); } }

		private ICore Core { get { return ServiceProvider.GetService<ICore>(); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public RemoteCore()
		{
			m_Originators = new CoreOriginatorCollection();
		}

		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			DisposeOriginators();
		}

		public void SetHostInfo(HostInfo source)
		{
			ApiClassInfo command =
				ApiCommandBuilder.NewCommand()
				                 .AtNode("ControlSystem")
				                 .AtNode("Core")
				                 .AtNodeGroup("Devices")
								 .Complete();

			RemoteApiMessage message = new RemoteApiMessage
			{
				Command = command
			};

			DirectMessageManager.Send<RemoteApiReply>(source, message, DevicesQueryResponse);
		}

		#region Private Methods

		private void DevicesQueryResponse(RemoteApiReply response)
		{
			ApiHandler.ReadResultsRecursive(response.Command, ParseResult);
		}

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

				LazyLoadProxyOriginator(id, kvp.Value);
			}
		}

		private void LazyLoadProxyOriginator(int id, ApiClassInfo classInfo)
		{
			if (m_Originators.ContainsChild(id))
				return;

			if (Core.Originators.ContainsChild(id))
				return;

			Type proxyType = classInfo.GetProxyTypes().FirstOrDefault();
			if (proxyType == null)
				return;

			IProxyOriginator originator = ReflectionUtils.CreateInstance<IProxyOriginator>(proxyType);
			originator.Id = id;
			originator.Name = classInfo.Name;

			Subscribe(originator);

			m_Originators.AddChild(originator);

			// Add to the core originator collection
			Core.Originators.AddChild(originator);
		}

		private void DisposeOriginators()
		{
			foreach (IProxyOriginator originator in m_Originators.OfType<IProxyOriginator>())
			{
				Unsubscribe(originator);

				if (originator is IDisposable)
					(originator as IDisposable).Dispose();
			}

			m_Originators.Clear();
		}

		#endregion

		#region Originator Callbacks

		private void Subscribe(IProxyOriginator originator)
		{
			originator.OnCommand += OriginatorOnCommand;
		}

		private void Unsubscribe(IProxyOriginator originator)
		{
			originator.OnCommand -= OriginatorOnCommand;
		}

		private void OriginatorOnCommand(object sender, ApiClassInfoEventArgs eventArgs)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
