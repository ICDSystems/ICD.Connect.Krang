﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICD.Common.Logging.Console;
using ICD.Common.Logging.Console.Loggers;
using ICD.Common.Permissions;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Connect.API;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Krang.Settings;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Themes;
using ICD.Connect.UI;

namespace ICD.Connect.Krang.Core
{
	public sealed class KrangBootstrap : IConsoleNode
	{
		private readonly KrangCore m_Core;

		#region Properties

		/// <summary>
		/// Gets the name of the node in the console.
		/// </summary>
		public string ConsoleName { get { return "ControlSystem"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return string.Empty; } }

		/// <summary>
		/// Gets the Krang instance.
		/// </summary>
		public KrangCore Krang { get { return m_Core; } }

		#endregion

		public KrangBootstrap()
		{
			AddServices();

			m_Core = new KrangCore { Serialize = true };
			m_Core.OnSettingsApplied += RoomOnSettingsApplied;

			ApiConsole.RegisterChild(this);

			IcdConsole.AddNewConsoleCommand(parameters => IcdConsole.ConsoleCommandResponse(CleanErrorLog(parameters)), "icderr",
			                                "Prints the error log without the added Crestron info",
			                                IcdConsole.eAccessLevel.Operator);
		}

		#region Methods

		public void Start()
		{
			ProgramUtils.PrintProgramInfoLine("Room Config", FileOperations.IcdConfigPath);
			try
			{
				FileOperations.LoadCoreSettings<KrangCore, KrangCoreSettings>(m_Core);
			}
			catch (Exception e)
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Error, e, "Exception in program initialization - {0}", e.Message);
			}
		}

		public void Stop()
		{
			try
			{
				Clear();
			}
			catch (Exception e)
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Error, e, "Exception in program stop - {0}", e.Message);
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Disposes all of the instantiated objects.
		/// </summary>
		private void Clear()
		{
			if (m_Core != null)
				m_Core.Dispose();
			ServiceProvider.DisposeStatic();
		}

		private void AddServices()
		{
			//Create and add default logger
			LoggingCore logger = new LoggingCore();
			logger.AddLogger(new IcdErrorLogger());
#if DEBUG
			logger.SeverityLevel = eSeverity.Debug;
#else
 			logger.SeverityLevel = eSeverity.Warning;
#endif
			ServiceProvider.AddService<ILoggerService>(logger);

			ServiceProvider.AddService(new DirectMessageManager());
			ServiceProvider.AddService(new BroadcastManager());

			ServiceProvider.AddService(new PermissionsManager());
		}

		private static string CleanErrorLog(params string[] args)
		{
			string errLog = string.Empty;
			IcdConsole.SendControlSystemCommand("err " + string.Join(" ", args), ref errLog);
			string cleaned = Regex.Replace(errLog,
			                               @"(^|\n)(?:\d+\. )?(Error|Notice|Info|Warning|Ok): (?:\w*)\.exe (?:\[(App \d+)\])? *# (.+?)  # ?",
			                               "$1$4 - $3 $2:: ");
			cleaned = Regex.Replace(cleaned,
			                        @"(?<!(^|\n)\d*)(?:\d+\. )?(Error|Notice|Info|Warning|Ok): SimplSharpPro\.exe ?(?:\[App (\d+)\] )?# (.+?)  # ?",
			                        "");
			return cleaned;
		}

		/// <summary>
		/// Called when settings are applied to the room.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void RoomOnSettingsApplied(object sender, EventArgs eventArgs)
		{
			BuildUserInterfaces();
		}

		/// <summary>
		/// Instantiates the user interfaces.
		/// </summary>
		private void BuildUserInterfaces()
		{
			foreach (ITheme factory in m_Core.Originators.GetChildren<ITheme>())
				factory.BuildUserInterfaces();
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Core", m_Core);
		}

		/// <summary>
		/// Gets the child console node groups.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return m_Core;
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("LoadCore", "Loads and applies the XML config.",
			                                () => m_Core.LoadSettings());
			yield return new ConsoleCommand("SaveCore", "Saves the current settings to XML",
			                                () => FileOperations.SaveSettings(m_Core.CopySettings()));
			yield return new ConsoleCommand("RebuildCore", "Rebuilds the core using the current settings.",
			                                () => FileOperations.ApplyCoreSettings(m_Core, m_Core.CopySettings()));
			yield return new ConsoleCommand("RebuildUis", "Rebuilds the UI instances", () => BuildUserInterfaces());
		}

		#endregion
	}
}
