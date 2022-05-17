using System;
using System.Collections.Generic;
using System.Linq;
#if !SIMPLSHARP
using System.Threading;
#endif
using ICD.Common.Logging;
using ICD.Common.Logging.Loggers;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Permissions;
using ICD.Common.Properties;
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
using ICD.Connect.Krang.Remote.Direct.API;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Originators;

namespace ICD.Connect.Krang.Cores
{
	[ApiClass("ControlSystem", null)]
	public sealed class KrangBootstrap : IConsoleNode
	{
		public event EventHandler<LifecycleStateEventArgs> OnCoreLifecycleStateChanged;

		private readonly ILoggingContext m_Logger;

#if NETSTANDARD
		// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
		private readonly Thread m_ConsoleThread;
		// ReSharper restore PrivateFieldCanBeConvertedToLocalVariable
#endif

		private KrangCore m_Core;

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
			RemoteApiCommandHandler.ControlSystem = this;
			ApiConsole.RegisterChild(this);

			m_Logger = new ServiceLoggingContext(this);

#if NETSTANDARD
			m_ConsoleThread = new Thread(ConsoleWorker)
			{
				IsBackground = true
			};

			if (IcdConsole.IsConsoleApp)
				m_ConsoleThread.Start();
#endif
		}

		#region Methods

		/// <summary>
		/// Load the core configuration.
		/// </summary>
		public void Start([CanBeNull] Action postApplyAction)
		{
			Stop();

			AddServices();

			m_Core = new KrangCore { Serialize = true };
			m_Core.OnLifecycleStateChanged += CoreOnLifecycleStateChanged;

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
				m_Logger.Log(eSeverity.Error, e, "Exception in program initialization");
			}
		}

		private void CoreOnLifecycleStateChanged(object sender, LifecycleStateEventArgs args)
		{
			OnCoreLifecycleStateChanged.Raise(this, new LifecycleStateEventArgs(args.Data));
		}

		/// <summary>
		/// Unload the core configuration.
		/// </summary>
		public void Stop()
		{
			try
			{
				if (DirectMessageManager != null)
					DirectMessageManager.Dispose();
				DirectMessageManager = null;

				if (BroadcastManager != null)
					BroadcastManager.Dispose();
				BroadcastManager = null;

				if (m_Core != null)
				{
					m_Core.OnLifecycleStateChanged -= CoreOnLifecycleStateChanged;
					m_Core.Dispose();
				}
				m_Core = null;

				// Avoid disposing the logging service
				foreach (object service in ServiceProvider.GetServices().Where(s => !(s is ILoggerService)))
				{
					ServiceProvider.RemoveService(service);

					IDisposable disposable = service as IDisposable;
					if (disposable != null)
						disposable.Dispose();
				}
			}
			catch (Exception e)
			{
				m_Logger.Log(eSeverity.Error, e, "Exception in program stop");
			}
		}

		#endregion

		#region Private Methods

		private void AddServices()
		{
			ServiceProvider
				.GetOrAddService<ILoggerService>(() =>
				{
					LoggingCore service = new LoggingCore
					{
						SeverityLevel =
#if DEBUG
							eSeverity.Debug
#else
							eSeverity.Notice
#endif
					};
					service.AddLogger(new IcdErrorLogger());
#if NETSTANDARD
					service.AddLogger(new FileLogger());
#endif
#if NETSTANDARD && RELEASE
					service.AddLogger(new EventLogLogger("ICD.Connect.Core-" + ProgramUtils.ProgramNumber));
#endif
					return service;
				});

			ServiceProvider.GetOrAddService<IActionSchedulerService>(() => new ActionSchedulerService());
			ServiceProvider.GetOrAddService(() => new PermissionsManager());

			DirectMessageManager = ServiceProvider.GetOrAddService(() => new DirectMessageManager());
			BroadcastManager = ServiceProvider.GetOrAddService(() => new BroadcastManager());
			SystemKeyManager = ServiceProvider.GetOrAddService(() => new SystemKeyManager());
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
#if !NETSTANDARD
			// Check for cpz files that are unextracted, indicating a problem
			if (IcdDirectory.GetFiles(PathUtils.ProgramPath, "*.cpz").Length != 0)
			{
				m_Logger.Log(eSeverity.Warning,
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
#pragma warning disable CS0618 // Type or member is obsolete
			if (systemKeyPath == null)
				systemKeyPath = IcdFile.Exists(FileOperations.LicensePath)
									? FileOperations.LicensePath
					                : null;
// ReSharper restore CSharpWarnings::CS0618
#pragma warning restore CS0618 // Type or member is obsolete

			// Revert back to the system key path for logging
			systemKeyPath = systemKeyPath ?? FileOperations.SystemKeyPath;

			ProgramUtils.PrintProgramInfoLine("System Key", systemKeyPath);

			SystemKeyManager.LoadSystemKey(systemKeyPath);
			return SystemKeyManager.IsValid();
		}

#if NETSTANDARD
		/// <summary>
		/// Handles user input while running from command line.
		/// </summary>
		private static void ConsoleWorker()
		{
			while (true)
			{
				string command = Console.ReadLine();
				if (command == null)
					continue;

				if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
				{
					Environment.Exit(0);
					return;
				}

				ApiConsole.ExecuteCommand(command);
			}
		}
#endif

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
