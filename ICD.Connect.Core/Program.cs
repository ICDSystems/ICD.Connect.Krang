#if !SIMPLSHARP
using System;
using ICD.Common.Utils;
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
		/// <summary>
		/// Run as service.
		/// </summary>
		public static void Main()
		{
			Options options = new Options();

			TopshelfExitCode rc = HostFactory.Run(x =>
			{
				x.EnableStartParameters();
				x.EnableSessionChanged();

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

				x.RunAsLocalSystem();

				x.SetDisplayName("ICD.Connect.Core");
				x.SetServiceName("ICD.Connect.Core");
				x.SetDescription("ICD Systems Core Application");

				x.SetStartTimeout(TimeSpan.FromMinutes(10));
				x.SetStopTimeout(TimeSpan.FromMinutes(10));

				x.StartAutomatically();

				x.BeforeInstall(() => BeforeInstall(options));
			});

			Environment.ExitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
		}

		private static KrangBootstrap Construct(Options options)
		{
			ProgramUtils.ProgramNumber = options.Program;

			bool isInteractive = IsConsoleApp();
			return new KrangBootstrap(isInteractive);
		}

		private static void Start(KrangBootstrap service)
		{
			IcdEnvironment.SetProgramStatus(IcdEnvironment.eProgramStatusEventType.Resumed);
			service.Start(null);
			IcdEnvironment.SetProgramInitializationComplete();
		}

		private static void Stop(KrangBootstrap service)
		{
			IcdEnvironment.SetProgramStatus(IcdEnvironment.eProgramStatusEventType.Stopping);
			service.Stop();
		}

		private static void HandleSessionChange(KrangBootstrap service, HostControl host, SessionChangedArguments args)
		{
			IcdEnvironment.HandleSessionChange(args.SessionId, (IcdEnvironment.eSessionChangeEventType)args.ReasonCode);
		}

		private static void BeforeInstall(Options options)
		{
			// Create registry key for event logging
			string key = string.Format(@"SYSTEM\CurrentControlSet\Services\EventLog\Application\ICD.Connect.Core-{0}", 1);
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

		private static bool IsConsoleApp()
		{
			try
			{
				// Hack
				int unused = Console.WindowHeight;
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
#endif
