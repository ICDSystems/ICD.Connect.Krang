using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Logging;
using ICD.Common.Logging.Loggers;
using ICD.Common.Permissions;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Services.Scheduler;
using ICD.Connect.API;
using ICD.Connect.API.Attributes;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.Cores
{
	[ApiClass("ControlSystem", null)]
	public sealed class KrangBootstrap : IConsoleNode
	{
		private readonly KrangCore m_Core;

		private ILoggerService m_Logger;
		private ActionSchedulerService m_ActionSchedulerService;

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

		/// <summary>
		/// Gets the broadcast manager instance.
		/// </summary>
		public BroadcastManager BroadcastManager { get; private set; }

		/// <summary>
		/// Gets the direct message manager instance.
		/// </summary>
		public DirectMessageManager DirectMessageManager { get; private set; }

		/// <summary>
		/// Gets the system key manager instance.
		/// </summary>
		public SystemKeyManager SystemKeyManager { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangBootstrap()
		{
			ApiHandler.ControlSystem = this;
			ApiConsole.RegisterChild(this);

			AddServices();

			m_Core = new KrangCore {Serialize = true};
		}

		#region Methods

		/// <summary>
		/// Load the core configuration.
		/// </summary>
		public void Start([CanBeNull] Action postApplyAction)
		{
			PrintProgramInfo();
			ValidateProgram();

// ReSharper disable UnusedVariable
			bool validSystemKey = ValidateSystemKey();
// ReSharper restore UnusedVariable
#if LICENSING
			if (!validSystemKey)
				return;
#endif

			try
			{
				m_Core.LoadSettings(postApplyAction);
			}
			catch (Exception e)
			{
				m_Logger.AddEntry(eSeverity.Error, e, "Exception in program initialization");
			}
		}

		/// <summary>
		/// Unload the core configuration.
		/// </summary>
		public void Stop()
		{
			try
			{
				DirectMessageManager.Dispose();
				BroadcastManager.Dispose();

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
 					eSeverity.Notice
#endif
			};
			logger.AddLogger(new IcdErrorLogger());

			m_Logger = logger;
			ServiceProvider.TryAddService<ILoggerService>(logger);

			DirectMessageManager = new DirectMessageManager();
			ServiceProvider.TryAddService(DirectMessageManager);

			BroadcastManager = new BroadcastManager();
			ServiceProvider.TryAddService(BroadcastManager);

			ServiceProvider.TryAddService(new PermissionsManager());

			SystemKeyManager = new SystemKeyManager();
			ServiceProvider.AddService(SystemKeyManager);

			m_ActionSchedulerService = new ActionSchedulerService();
			ServiceProvider.TryAddService<IActionSchedulerService>(m_ActionSchedulerService);
		}

		private void PrintProgramInfo()
		{
			ProgramUtils.PrintProgramInfoLine("Configuration",
#if DEBUG
			                                  "Debug"
#else
			                                  "Release"
#endif
);

			ProgramUtils.PrintProgramInfoLine("System Config", FileOperations.SystemConfigPath);
		}

		/// <summary>
		/// Simple check and log that determines if there is a CPZ in the program directory.
		/// </summary>
		private void ValidateProgram()
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
		}

		/// <summary>
		/// Returns true if we loaded a valid license file.
		/// </summary>
		/// <returns></returns>
		private bool ValidateSystemKey()
		{
			string systemKeyPath = IcdFile.Exists(FileOperations.SystemKeyPath)
				                       ? FileOperations.SystemKeyPath
				                       : null;

			// Backwards compatibility
// ReSharper disable CSharpWarnings::CS0618
			if (systemKeyPath == null)
				systemKeyPath = IcdFile.Exists(FileOperations.LicensePath)
					                ? FileOperations.LicensePath
					                : null;
// ReSharper restore CSharpWarnings::CS0618

			// Revert back to the system key path for logging
			systemKeyPath = systemKeyPath ?? FileOperations.SystemKeyPath;

			ProgramUtils.PrintProgramInfoLine("System Key", systemKeyPath);

			SystemKeyManager.LoadSystemKey(systemKeyPath);
			return SystemKeyManager.IsValid();
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			KrangBootstrapConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console node groups.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			return KrangBootstrapConsole.GetConsoleNodes(this);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			return KrangBootstrapConsole.GetConsoleCommands(this);
		}

		#endregion
	}
}
