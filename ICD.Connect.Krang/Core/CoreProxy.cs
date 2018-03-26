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

namespace ICD.Connect.Krang.Core
{
	/// <summary>
	/// CoreProxy simply represents an abstract core instance that lives remotely.
	/// </summary>
	public sealed class CoreProxy : AbstractCore<CoreProxySettings>
	{
		private readonly CoreOriginatorCollection m_Originators;

		private DirectMessageManager DirectMessageManager { get { return ServiceProvider.GetService<DirectMessageManager>(); } }

		private RemoteApiResultHandler ApiResultHandler { get { return ServiceProvider.GetService<RemoteApiResultHandler>(); } }

		private ICore Core { get { return ServiceProvider.GetService<ICore>(); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public CoreProxy()
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
				LazyLoadProxyOriginator((int)kvp.Key + 1, kvp.Value);
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

			IOriginator originator = ReflectionUtils.CreateInstance<IOriginator>(proxyType);
			originator.Id = id;
			originator.Name = classInfo.Name;

			m_Originators.AddChild(originator);

			// Add to the core originator collection
			Core.Originators.AddChild(originator);
		}

		private void DisposeOriginators()
		{
			foreach (IDisposable originator in m_Originators.OfType<IDisposable>())
				originator.Dispose();
			m_Originators.Clear();
		}
	}
}
