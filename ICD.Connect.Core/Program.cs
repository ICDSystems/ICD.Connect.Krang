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
				});

				x.RunAsNetworkService();

				x.SetDisplayName("ICD.Connect.Core");
				x.SetServiceName("ICD.Connect.Core");
				x.SetDescription("ICD Systems Core Application");

				x.SetStartTimeout(TimeSpan.FromMinutes(10));
				x.SetStopTimeout(TimeSpan.FromMinutes(10));

				x.StartAutomatically();

				x.BeforeInstall(() => BeforeInstall(options));
			});

			int exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
			Environment.ExitCode = exitCode;
		}

		private static KrangBootstrap Construct(Options options)
		{
			ProgramUtils.ProgramNumber = options.Program;

			bool isInteractive = IsConsoleApp();
			return new KrangBootstrap(isInteractive);
		}

		private static void Start(KrangBootstrap service)
		{
			service.Start(null);
			IcdEnvironment.SetProgramInitializationComplete();
		}

		private static void Stop(KrangBootstrap service)
		{
			service.Stop();
		}

		private static void BeforeInstall(Options options)
		{
			// Create registry key for event logging
			string key = @"SYSTEM\CurrentControlSet\Services\EventLog\Application\ICD.Connect.Core-" + options.Program;
			if (Registry.LocalMachine.OpenSubKey(key, true) == null)
				Registry.LocalMachine.CreateSubKey(key);
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
