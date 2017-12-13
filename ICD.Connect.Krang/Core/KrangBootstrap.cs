﻿using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp.CrestronIO;
#if SIMPLSHARP
using Crestron.SimplSharp.Reflection;
#else
using System.Reflection;
#endif
using ICD.Common.Logging.Console;
using ICD.Common.Logging.Console.Loggers;
using ICD.Common.Permissions;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Krang.Settings;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Common.Utils.IO;

namespace ICD.Connect.Krang.Core
{
	public sealed class KrangBootstrap : IConsoleNode
	{
		private readonly KrangCore m_Core;

		private DirectMessageManager m_DirectMessageManager;
		private BroadcastManager m_BroadcastManager;

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

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangBootstrap()
		{
			AddServices();

			m_Core = new KrangCore { Serialize = true };

			ApiConsole.RegisterChild(this);
		}

		#region Methods

		public void Start()
		{
            // Check for cpz files that are unextracted, indicating a problem
		    if (IcdDirectory.GetFiles(PathUtils.ProgramPath, "*.cpz").Length != 0)
		    {
		        ServiceProvider.TryGetService<ILoggerService>()
                               .AddEntry(eSeverity.Warning, "A CPZ FILE STILL EXISTS IN THE PROGRAM DIRECTORY. YOU MAY WISH TO VALIDATE THAT THE CORRECT PROGRAM IS RUNNING.");
		    }
			ProgramUtils.PrintProgramInfoLine("Room Config", FileOperations.IcdConfigPath);
			try
			{
				FileOperations.LoadCoreSettings<KrangCore, KrangCoreSettings>(m_Core);
			}
			catch (Exception e)
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Error, e, "Exception in program initialization");
			}
		}

		public void Stop()
		{
			try
			{
				m_DirectMessageManager.Dispose();
				m_BroadcastManager.Dispose();

				Clear();
			}
			catch (Exception e)
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Error, e, "Exception in program stop");
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

			// Avoid disposing the logging service
			foreach (object service in ServiceProvider.GetServices().Where(s => !(s is ILoggerService)))
			{
				ServiceProvider.RemoveService(service);

				IDisposable disposable = service as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
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
			ServiceProvider.TryAddService<ILoggerService>(logger);

			m_DirectMessageManager = new DirectMessageManager();
			ServiceProvider.AddService(m_DirectMessageManager);

			m_BroadcastManager = new BroadcastManager();
			ServiceProvider.TryAddService(m_BroadcastManager);

			ServiceProvider.TryAddService(new PermissionsManager());
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
			yield return new ConsoleCommand("SaveCore", "Saves the current settings to XML.",
			                                () => FileOperations.SaveSettings(m_Core.CopySettings()));
			yield return new ConsoleCommand("RebuildCore", "Rebuilds the core using the current settings.",
			                                () => FileOperations.ApplyCoreSettings(m_Core, m_Core.CopySettings()));

			yield return new ConsoleCommand("PrintPlugins", "Prints the loaded plugin assemblies.",
											() => PrintPlugins());
			yield return new ConsoleCommand("PrintTypes", "Prints the loaded device types.",
											() => PrintTypes());
		}

		private static string PrintPlugins()
		{
			TableBuilder builder = new TableBuilder("Assembly", "Path", "Version", "Date");

			foreach (Assembly assembly in PluginFactory.GetFactoryAssemblies().OrderBy(a => a.FullName))
			{
				string name = assembly.GetName().Name;
				string path = assembly.GetPath();
				string version = assembly.GetName().Version.ToString();
				DateTime date = IcdFile.GetLastWriteTime(path);

				path = IcdPath.GetDirectoryName(path);

				builder.AddRow(name, path, version, date);
			}

			return builder.ToString();
		}

		private static string PrintTypes()
		{
			TableBuilder builder = new TableBuilder("Type", "Assembly", "Path", "Version", "Date");

			foreach (string factoryName in PluginFactory.GetFactoryNames().Order())
			{
				Assembly assembly = PluginFactory.GetType(factoryName).GetAssembly();

				string name = assembly.GetName().Name;
				string path = assembly.GetPath();
				string version = assembly.GetName().Version.ToString();
				DateTime date = IcdFile.GetLastWriteTime(path);

				path = IcdPath.GetDirectoryName(path);

				builder.AddRow(factoryName, name, path, version, date);
			}

			return builder.ToString();
		}

		#endregion
	}
}
