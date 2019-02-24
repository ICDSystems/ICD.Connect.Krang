using System;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Ports.Tcp;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.SPlus.IcdConsoleServer
{
	public sealed class IcdConsoleServerDevice : AbstractDevice<IcdConsoleServerSettings>
	{
		private readonly Regex m_NewlineRegex;

		private AsyncTcpServer m_TcpServer;

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return true;
		}

		public IcdConsoleServerDevice()
		{
			m_NewlineRegex = new Regex("(?<!\r)\n");
		}

		#region TCP Server Callbacks

		private void Subscribe(AsyncTcpServer tcpServer)
		{
			if (tcpServer == null)
				return;

			tcpServer.OnSocketStateChange += TcpServerOnSocketStateChange;
			tcpServer.OnDataReceived += TcpServerOnDataReceived;
		}

		private void Unsubscribe(AsyncTcpServer tcpServer)
		{
			if (tcpServer == null)
				return;

			tcpServer.OnSocketStateChange -= TcpServerOnSocketStateChange;
			tcpServer.OnDataReceived -= TcpServerOnDataReceived;
		}

		private void TcpServerOnSocketStateChange(object sender, SocketStateEventArgs args)
		{
			if (args.SocketState != SocketStateEventArgs.eSocketStatus.SocketStatusConnected || args.ClientId == 0)
				return;

			m_TcpServer.Send(args.ClientId,"Welcome to IcdConsole. All commands begin with `ICD`. Type `ICD ?` for help\r\n");
		}

		private void TcpServerOnDataReceived(object sender, TcpReceiveEventArgs args)
		{
			string commandString = args.Data.Trim();

			commandString = string.IsNullOrEmpty(commandString) ? ApiConsole.HELP_COMMAND : commandString;

			// User convenience, let them know there's actually a UCMD handler
			if (commandString == ApiConsole.HELP_COMMAND)
			{
				CommandResponse(args.ClientId,String.Format("Type \"{0} {1}\" to see commands registered for {2}", ApiConsole.ROOT_COMMAND, ApiConsole.HELP_COMMAND,
												  typeof(IcdConsole).Name));
				return;
			}

			// Only care about commands that start with ICD prefix.
			if (!commandString.Equals(ApiConsole.ROOT_COMMAND, StringComparison.OrdinalIgnoreCase) &&
				!commandString.StartsWith(ApiConsole.ROOT_COMMAND + ' ', StringComparison.OrdinalIgnoreCase))
				return;

			// Trim the prefix
			commandString = commandString.Substring(ApiConsole.ROOT_COMMAND.Length).Trim();

			CommandResponse(args.ClientId, ApiConsole.ExecuteCommandForResponse(commandString));
		}

		#endregion

		#region ICDConsole Callback

		private void IcdConsoleOnConsolePrint(object sender, StringEventArgs args)
		{
			if (m_TcpServer == null)
				return;

			
				m_TcpServer.Send(m_NewlineRegex.Replace(args.Data,"\r\n"));
		}

		#endregion

		#region Private Methods

		private void CommandResponse(uint client, string message)
		{
			if (m_TcpServer == null)
				return;

			if (!m_TcpServer.ClientConnected(client))
				return;

			m_TcpServer.Send(client, m_NewlineRegex.Replace(message, "\r\n") + "\r\n");
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(IcdConsoleServerSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.PortNumber = m_TcpServer != null ? m_TcpServer.Port : settings.DefaultPortNumber;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(IcdConsoleServerSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_TcpServer = new AsyncTcpServer();
			m_TcpServer.Port = settings.PortNumber;
			m_TcpServer.MaxNumberOfClients = 25;
			Subscribe(m_TcpServer);

			m_TcpServer.Start();

			IcdConsole.OnConsolePrint += IcdConsoleOnConsolePrint;

		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			IcdConsole.OnConsolePrint -= IcdConsoleOnConsolePrint;

			if (m_TcpServer != null)
			{
				m_TcpServer.Stop();
			}
			Unsubscribe(m_TcpServer);

			m_TcpServer = null;
		}

		#endregion
	}
}