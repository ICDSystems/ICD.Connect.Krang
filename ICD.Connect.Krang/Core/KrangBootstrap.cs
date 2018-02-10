using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Logging.Console;
using ICD.Common.Logging.Console.Loggers;
using ICD.Common.Permissions;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API;
using ICD.Connect.API.Attributes;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;
#if SIMPLSHARP
using Crestron.SimplSharp.Reflection;
#else
using System.Reflection;
#endif

namespace ICD.Connect.Krang.Core
{
	[ApiClass("ControlSystem", null)]
	public sealed class KrangBootstrap : IConsoleNode
	{
		private readonly KrangCore m_Core;

		private ILoggerService m_Logger;
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
		[ApiNode("Core", "The core instance for this program")]
		public KrangCore Krang { get { return m_Core; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangBootstrap()
		{
			AddServices();

			m_Core = new KrangCore {Serialize = true};

			ApiConsole.RegisterChild(this);
		}

		#region Methods

		public void Start()
		{
#if SIMPLSHARP
			// Check for cpz files that are unextracted, indicating a problem
			if (IcdDirectory.GetFiles(PathUtils.ProgramPath, "*.cpz").Length != 0)
			{
				m_Logger.AddEntry(eSeverity.Warning,
				                  "A CPZ FILE STILL EXISTS IN THE PROGRAM DIRECTORY." +
								  " YOU MAY WISH TO VALIDATE THAT THE CORRECT PROGRAM IS RUNNING.");
			}
#endif
			ProgramUtils.PrintProgramInfoLine("Room Config", FileOperations.IcdConfigPath);

			try
			{
				FileOperations.LoadCoreSettings<KrangCore, KrangCoreSettings>(m_Core);
			}
			catch (Exception e)
			{
				m_Logger.AddEntry(eSeverity.Error, e, "Exception in program initialization");
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
				m_Logger.AddEntry(eSeverity.Error, e, "Exception in program stop");
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
			// Create and add default logger
			LoggingCore logger = new LoggingCore
			{
				SeverityLevel =
#if DEBUG
					eSeverity.Debug
#else
 					eSeverity.Warning
#endif
			};
			logger.AddLogger(new IcdErrorLogger());

			m_Logger = logger;
			ServiceProvider.TryAddService<ILoggerService>(logger);

			m_DirectMessageManager = new DirectMessageManager();
			ServiceProvider.TryAddService(m_DirectMessageManager);

			m_BroadcastManager = new BroadcastManager();
			ServiceProvider.TryAddService(m_BroadcastManager);

			ServiceProvider.TryAddService(new PermissionsManager());
		}

		#endregion

		#region API

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
			yield return new ConsoleCommand("LoadCore", "Loads and applies the XML config.", () => LoadSettings());
			yield return new ConsoleCommand("SaveCore", "Saves the current settings to XML.", () => SaveSettings());
			yield return new ConsoleCommand("RebuildCore", "Rebuilds the core using the current settings.", () => RebuildCore());
			yield return new ConsoleCommand("PrintPlugins", "Prints the loaded plugin assemblies.", () => PrintPlugins());
			yield return new ConsoleCommand("PrintTypes", "Prints the loaded device types.", () => PrintTypes());
		}

		[ApiMethod("LoadSettings", "Loads the local config file and applies it to the current Core instance.")]
		private void LoadSettings()
		{
			m_Core.LoadSettings();
		}

		[ApiMethod("SaveSettings", "Saves the current Core instance to the local config file.")]
		private void SaveSettings()
		{
			// Saving settings involves running some console commands to get processor information.
			// Executing console commands from a console command thread is extremely slow.
			ThreadingUtils.SafeInvoke(() =>
			                          {
				                          ICoreSettings settings = m_Core.CopySettings();
				                          FileOperations.SaveSettings(settings);
			                          });
		}

		[ApiMethod("RebuildCore", "Loads the local config file and applies it to the current Core instance.")]
		private void RebuildCore()
		{
			FileOperations.ApplyCoreSettings(m_Core, m_Core.CopySettings());
		}

		[ApiMethod("GetPlugins", "Returns a table of the loaded plugin assemblies.")]
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

		[ApiMethod("GetOriginatorTypes", "Returns a table of the loaded plugin originators.")]
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
