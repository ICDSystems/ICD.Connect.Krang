using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.Logging.Console;
using ICD.Common.Logging.Console.Loggers;
using ICD.Common.Permissions;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
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
		private const string NVRAM_FILE = "NVRAM_DEPRECATED";

		private readonly KrangCore m_Core;

		private ILoggerService m_Logger;
		private DirectMessageManager m_DirectMessageManager;
		private BroadcastManager m_BroadcastManager;
		private LicenseManager m_LicenseManager;
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
		public BroadcastManager BroadcastManager { get { return m_BroadcastManager; } }

		/// <summary>
		/// Gets the license manager instance.
		/// </summary>
		public LicenseManager LicenseManager { get { return m_LicenseManager; } }

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

			ProgramUtils.PrintProgramInfoLine("License", FileOperations.LicensePath);
			ProgramUtils.PrintProgramInfoLine("Room Config", FileOperations.IcdConfigPath);

			CreateNvramDeprecatedFile();

			var nvramCommonConfig = IcdPath.Combine(IcdPath.Combine(PathUtils.RootPath, "NVRAM"), "CommonConfig");
			MigrateDirectory(nvramCommonConfig, PathUtils.CommonConfigPath);

			var nvramProgramConfig = IcdPath.Combine(IcdPath.Combine(PathUtils.RootPath, "NVRAM"),
				string.Format("Program{0:D2}Config", ProgramUtils.ProgramNumber));
			MigrateDirectory(nvramProgramConfig, PathUtils.ProgramConfigPath);

			try
			{
#if LICENSING
				m_LicenseManager.LoadLicense(FileOperations.LicensePath);
				if (m_LicenseManager.IsValid())
#endif
					m_Core.LoadSettings();
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

			m_LicenseManager = new LicenseManager();
			ServiceProvider.AddService(m_LicenseManager);

			m_ActionSchedulerService = new ActionSchedulerService();
			ServiceProvider.TryAddService<IActionSchedulerService>(m_ActionSchedulerService);
		}

		#endregion

		#region Migration

		/// <summary>
		/// Copies all the files and folders from oldDirectory to newDirectory, creating folders if needed.
		/// Does not remove the files/folders at oldDirectory.
		/// </summary>
		/// <param name="oldDirectory"></param>
		/// <param name="newDirectory"></param>
		public void MigrateDirectory(string oldDirectory, string newDirectory)
		{
			// abandon if new folder exists and isn't empty
			if (IcdDirectory.Exists(newDirectory) &&
			    (IcdDirectory.GetFiles(newDirectory).Length > 0 || IcdDirectory.GetDirectories(newDirectory).Length > 0))
				return;

			// abandon if old folder is empty or doesn't exist
			if (!IcdDirectory.Exists(oldDirectory) ||
			    (IcdDirectory.GetFiles(oldDirectory).Length == 0 && IcdDirectory.GetDirectories(oldDirectory).Length == 0))
				return;

			m_Logger.AddEntry(eSeverity.Informational, "Migrating {0} to {1}", oldDirectory, newDirectory);

			// migrate files
			foreach (var oldFile in IcdDirectory.GetFiles(oldDirectory))
			{
				var relativePath = IcdPath.GetRelativePath(oldDirectory, oldFile);
				var newFile = IcdPath.Combine(newDirectory, relativePath);

				if (!IcdDirectory.Exists(newDirectory))
					IcdDirectory.CreateDirectory(newDirectory);

				// copy file
				IcdFile.Copy(oldFile, newFile);
			}
			// migrate directories
			foreach (var oldSubdirectory in IcdDirectory.GetDirectories(oldDirectory))
			{
				var relativePath = IcdPath.GetRelativePath(oldDirectory, oldSubdirectory);
				var newSubdirectory = IcdPath.Combine(newDirectory, relativePath);

				MigrateDirectory(oldSubdirectory, newSubdirectory);
			}
		}

		/// <summary>
		/// Creates a file with info about the NVRAM deprection in the NVRAM folder.
		/// Does not override if file exists.
		/// </summary>
		public void CreateNvramDeprecatedFile()
		{
			string directory = IcdPath.Combine(PathUtils.RootPath, "NVRAM");
			string deprecationFile = IcdPath.Combine(directory, NVRAM_FILE);

			if (!IcdDirectory.Exists(directory) || IcdFile.Exists(deprecationFile))
				return;

			try
			{
				string subDirectory = PathUtils.RootConfigPath.Remove(PathUtils.RootPath);

				using (IcdFileStream stream = IcdFile.Create(deprecationFile))
				{
					string data = string.Format("The 'NVRAM' directory has been deprecated in favor of the '{0}' directory",
												subDirectory);
					stream.Write(data, Encoding.UTF8);
				}
			}
			catch (Exception e)
			{
				m_Logger.AddEntry(eSeverity.Error, e, "Error writing NVRAM Deprecated File");
			}
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
