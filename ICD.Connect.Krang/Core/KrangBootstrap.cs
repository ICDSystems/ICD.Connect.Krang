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
			ApiHandler.ControlSystem = this;
			ApiConsole.RegisterChild(this);

			AddServices();

			m_Core = new KrangCore {Serialize = true};
		}

		#region Methods

		/// <summary>
		/// Load the core configuration.
		/// </summary>
		public void Start()
		{
			ValidateProgram();

			MigrateNvram();

			ProgramUtils.PrintProgramInfoLine("License", FileOperations.LicensePath);
			if (!ValidateLicense())
				return;

			ProgramUtils.PrintProgramInfoLine("Room Config", FileOperations.IcdConfigPath);

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

		#region Migration

		/// <summary>
		/// Migrates the contents of NVRAM to the USER directory.
		/// </summary>
		private void MigrateNvram()
		{
			string nvramPath = PathUtils.Join(PathUtils.RootPath, "NVRAM");
			if (!IcdDirectory.Exists(nvramPath))
				return;

			string configPath = PathUtils.RootConfigPath;
			if (configPath == nvramPath)
				return;

			// Abandon if new folder exists and isn't empty
			if (IcdDirectory.Exists(configPath) &&
				(IcdDirectory.GetFiles(configPath).Length > 0 || IcdDirectory.GetDirectories(configPath).Length > 0))
				return;

			m_Logger.AddEntry(eSeverity.Informational, "Migrating {0} to {1}", nvramPath, configPath);

			bool migrated = MigrateDirectory(nvramPath, configPath);
			if (migrated)
				CreateNvramDeprecatedFile();
		}

		/// <summary>
		/// Copies all the files and folders from oldDirectory to newDirectory, creating folders if needed.
		/// Does not remove the files/folders at oldDirectory.
		/// </summary>
		/// <param name="oldPath"></param>
		/// <param name="newPath"></param>
		private static bool MigrateDirectory(string oldPath, string newPath)
		{
			bool migrated = false;

			// Migrate files
			foreach (string oldFile in IcdDirectory.GetFiles(oldPath))
			{
				string relativePath = IcdPath.GetRelativePath(oldPath, oldFile);
				string newFile = IcdPath.Combine(newPath, relativePath);

				migrated |= MigrateFile(oldFile, newFile);
			}

			// Migrate directories
			foreach (string oldSubdirectory in IcdDirectory.GetDirectories(oldPath))
			{
				string relativePath = IcdPath.GetRelativePath(oldPath, oldSubdirectory);
				string newSubdirectory = IcdPath.Combine(newPath, relativePath);

				migrated |= MigrateDirectory(oldSubdirectory, newSubdirectory);
			}

			return migrated;
		}

		/// <summary>
		/// Copies the file at the old path to the new path.
		/// Does nothing if a file already exists at the new path.
		/// </summary>
		/// <param name="oldPath"></param>
		/// <param name="newPath"></param>
		/// <returns></returns>
		private static bool MigrateFile(string oldPath, string newPath)
		{
			if (!IcdFile.Exists(oldPath))
				throw new InvalidOperationException("File does not exist");

			if (IcdFile.Exists(newPath))
				return false;

			string directory = IcdPath.GetDirectoryName(newPath);
			IcdDirectory.CreateDirectory(directory);

			// Copy file
			IcdFile.Copy(oldPath, newPath);

			return true;
		}

		/// <summary>
		/// Creates a file with info about the NVRAM deprection in the NVRAM folder.
		/// Does not override if file exists.
		/// </summary>
		private static void CreateNvramDeprecatedFile()
		{
			string directory = IcdPath.Combine(PathUtils.RootPath, "NVRAM");
			if (!IcdDirectory.Exists(directory))
				return;

			string deprecationFile = IcdPath.Combine(directory, NVRAM_FILE);
			if (IcdFile.Exists(deprecationFile))
				return;

			string subDirectory = PathUtils.RootConfigPath.Remove(PathUtils.RootPath);

			using (IcdFileStream stream = IcdFile.Create(deprecationFile))
			{
				string data = string.Format("The 'NVRAM' directory has been deprecated in favor of the '{0}' directory",
											subDirectory);
				stream.Write(data, Encoding.UTF8);
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
