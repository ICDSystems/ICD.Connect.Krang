#if NETCOREAPP
using System;
using System.Threading;
using System.Threading.Tasks;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Krang.Cores;
using Microsoft.Win32;
using Topshelf;
using Topshelf.StartParameters;

namespace ICD.Connect.Core
{
	internal sealed class Options
	{
		public uint Program { get; set; } = 1;
	}

	internal static class Program
	{
		private const string SERVICE_NAME = "ICD.Connect";
		private const string SERVICE_DESCRIPTION = "ICD Systems IoT Management Service";

		private static CancellationTokenSource s_MainTaskCancellationTokenSource;

		private static HostControl s_HostControl;

		// ReSharper disable NotAccessedField.Local
		private static Task s_MainTask;
		// ReSharper restore NotAccessedField.Local

		/// <summary>
		/// Static constructor.
		/// </summary>
		static Program()
		{
			Win32.OnServiceControlStopShutdown += Win32OnServiceControlStopShutdown;
			Win32.OnSystemDeviceAdded += Win32OnSystemDeviceAdded;
			Win32.OnSystemDeviceRemoved += Win32OnSystemDeviceRemoved;
		}

		/// <summary>
		/// Run as service.
		/// </summary>
		public static void Main()
		{
			if (IcdConsole.IsConsoleApp)
				AnsiUtils.EnableAnsiColor();

			Options options = new Options();

			TopshelfExitCode code = HostFactory.Run(x =>
			{
				x.EnableStartParameters();
				x.EnableSessionChanged();
				x.EnableShutdown();

				//x.EnableServiceRecovery(rc =>
				//{
				//	rc.OnCrashOnly();
				//	rc.RestartService(0);
				//	rc.SetResetPeriod(1);
				//});

				x.WithStartParameter("program", p =>
				{
					uint program;
					options.Program = uint.TryParse(p, out program) ? program : 1;
				});

				x.Service<KrangBootstrap>(s =>
				{
					s.ConstructUsing(n => Construct(options));
					s.WhenStarted(Start);
					s.WhenStopped(Stop);
					s.WhenSessionChanged(HandleSessionChange);
				});

				x.OnException(e =>
				{
					ServiceProvider.TryGetService<ILoggerService>()
					               ?.AddEntry(eSeverity.Error, e, "Unhandled exception - {0}", e.Message);
				});

				x.RunAsLocalSystem();

				x.SetServiceName(SERVICE_NAME);
				x.SetDisplayName(SERVICE_NAME);
				x.SetDescription(SERVICE_DESCRIPTION);

				x.SetStartTimeout(TimeSpan.FromMinutes(10));
				x.SetStopTimeout(TimeSpan.FromMinutes(10));

				x.StartAutomatically();

				x.BeforeInstall(() => BeforeInstall(options));

				x.DependsOnEventLog();
			});

			ServiceProvider.TryGetService<ILoggerService>()?.Flush();

			Environment.ExitCode = (int)Convert.ChangeType(code, code.GetTypeCode());
		}

		#region Service

		/// <summary>
		/// Creates the service instance.
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		private static KrangBootstrap Construct(Options options)
		{
			ProgramUtils.ProgramNumber = options.Program;

			return new KrangBootstrap();
		}

		/// <summary>
		/// Starts the service.
		/// </summary>
		/// <param name="service"></param>
		/// <param name="hostControl"></param>
		/// <returns></returns>
		private static bool Start(KrangBootstrap service, HostControl hostControl)
		{
			s_HostControl = hostControl;

			if (!IcdConsole.IsConsoleApp)
				Win32.RegisterDeviceNotifications(SERVICE_NAME);

			s_MainTaskCancellationTokenSource = new CancellationTokenSource();

			s_MainTask =
				Task.Factory.StartNew(() =>
				{
					try
					{
						IcdEnvironment.SetProgramStatus(IcdEnvironment.eProgramStatusEventType.Resumed);
						service.Start(null);
						IcdEnvironment.SetProgramInitializationComplete();
					}
					catch (Exception e)
					{
						ServiceProvider.TryGetService<ILoggerService>()?.AddEntry(eSeverity.Error, e, "Failed to start service - {0}", e.Message);
						hostControl.Stop();
					}
				},
				s_MainTaskCancellationTokenSource.Token);

			return true;
		}

		/// <summary>
		/// Stops the service.
		/// </summary>
		/// <param name="service"></param>
		/// <param name="hostControl"></param>
		/// <returns></returns>
		private static bool Stop(KrangBootstrap service, HostControl hostControl)
		{
			s_MainTaskCancellationTokenSource?.Cancel();

			try
			{
				Win32.UnregisterDeviceNotifications();
				IcdEnvironment.SetProgramStatus(IcdEnvironment.eProgramStatusEventType.Stopping);
				service.Stop();
			}
			catch (Exception e)
			{
				ServiceProvider.TryGetService<ILoggerService>()?.AddEntry(eSeverity.Error, e, "Failed to stop service - {0}", e.Message);
				// Don't tell hostControl to stop - ends up calling this method recursively
				//hostControl.Stop();
			}

			return true;
		}

		/// <summary>
		/// Called before the service is installed.
		/// </summary>
		/// <param name="options"></param>
		private static void BeforeInstall(Options options)
		{
			// Create registry key for event logging
			string key = string.Format(@"SYSTEM\CurrentControlSet\Services\EventLog\Application\ICD.Connect.Core-{0}", options.Program);
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(key, true) ?? Registry.LocalMachine.CreateSubKey(key);
			if (registryKey == null)
				throw new ApplicationException("Failed to create registry key");

			// Setup the event log message file
			// Hack - Just using the regular .Net Runtime messages
			const string eventMessageFilePath = @"C:\Windows\System32\mscoree.dll";
			if (registryKey.GetValue("EventMessageFile") == null)
				registryKey.SetValue("EventMessageFile", eventMessageFilePath, RegistryValueKind.String);

			// Also copied from .Net Runtime
			if (registryKey.GetValue("TypesSupported") == null)
				registryKey.SetValue("TypesSupported", 0x07, RegistryValueKind.DWord);
		}

		#endregion

		#region System Callbacks

		/// <summary>
		/// Called when the session changes (log out, log in, etc).
		/// </summary>
		/// <param name="service"></param>
		/// <param name="host"></param>
		/// <param name="args"></param>
		private static void HandleSessionChange(KrangBootstrap service, HostControl host, SessionChangedArguments args)
		{
			IcdEnvironment.HandleSessionChange(args.SessionId, (IcdEnvironment.eSessionChangeEventType)args.ReasonCode);
		}

		/// <summary>
		/// Called when the service is signaled to stop or shutdown.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void Win32OnServiceControlStopShutdown(object sender, EventArgs e)
		{
			s_HostControl?.Stop();
		}

		/// <summary>
		/// Called when a device is connected to the system.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void Win32OnSystemDeviceAdded(object sender, EventArgs e)
		{
			IcdEnvironment.RaiseSystemDeviceAddedEvent();
		}

		/// <summary>
		/// Called when a device is disconnected from the system.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void Win32OnSystemDeviceRemoved(object sender, EventArgs e)
		{
			IcdEnvironment.RaiseSystemDeviceRemovedEvent();
		}

		#endregion
	}
}
#endif
