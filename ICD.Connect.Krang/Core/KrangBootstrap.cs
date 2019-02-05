using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Logging.Console;
using ICD.Common.Logging.Console.Loggers;
using ICD.Common.Permissions;
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

namespace ICD.Connect.Krang.Core
{
	[ApiClass("ControlSystem", null)]
	public sealed class KrangBootstrap : IConsoleNode
	{
		private readonly KrangCore m_Core;

		private ILoggerService m_Logger;
		private DirectMessageManager m_DirectMessageManager;
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
		/// Gets the license manager instance.
		/// </summary>
		public LicenseManager LicenseManager { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangBootstrap()
		{
			AddServices();

			m_Core = new KrangCore {Serialize = true};

			ApiHandler.ControlSystem = this;
			ApiConsole.RegisterChild(this);
		}

		#region Methods

		/// <summary>
		/// Load the core configuration.
		/// </summary>
		public void Start()
		{
			PrintProgramInfo();
			ValidateProgram();

			NvramMigration.Migrate(m_Logger);

			try
			{
				m_Core.LoadSettings();
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
				m_DirectMessageManager.Dispose();
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
 					eSeverity.Warning
#endif
			};
			logger.AddLogger(new IcdErrorLogger());

			m_Logger = logger;
			ServiceProvider.TryAddService<ILoggerService>(logger);

			m_DirectMessageManager = new DirectMessageManager();
			ServiceProvider.TryAddService(m_DirectMessageManager);

			BroadcastManager = new BroadcastManager();
			ServiceProvider.TryAddService(BroadcastManager);

			ServiceProvider.TryAddService(new PermissionsManager());

			LicenseManager = new LicenseManager();
			ServiceProvider.AddService(LicenseManager);

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

#if LICENSING
			ProgramUtils.PrintProgramInfoLine("License", FileOperations.LicensePath);
			if (!ValidateLicense())
				return;
#endif

			ProgramUtils.PrintProgramInfoLine("Room Config", FileOperations.IcdConfigPath);
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
		private bool ValidateLicense()
		{
#if LICENSING
			m_LicenseManager.LoadLicense(FileOperations.LicensePath);
			return m_LicenseManager.IsValid();
#else
			return true;
#endif
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
