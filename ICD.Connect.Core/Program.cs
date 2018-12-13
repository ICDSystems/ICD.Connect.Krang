#if !SIMPLSHARP
using System;
using ICD.Connect.API;
using ICD.Connect.Krang.Core;
using CommandLine;
using ICD.Common.Properties;
using ICD.Common.Utils;

namespace ICD.Connect.Core
{
	[UsedImplicitly]
	internal sealed class Options
	{
		[Option('p', "program", Default=(uint)1, Required=false, HelpText="Specifies the program number.")]
		public uint Program { get; set; }
	}

	internal static class Program
	{
		private static readonly KrangBootstrap s_Bootstrap;

		/// <summary>
		/// Constructor.
		/// </summary>
		static Program()
		{
			s_Bootstrap = new KrangBootstrap();
			Console.CancelKeyPress += (a, b) => s_Bootstrap.Stop();
		}

		/// <summary>
		/// Program entry point.
		/// </summary>
		/// <param name="args"></param>
		private static void Main(string[] args)
		{
			Parser.Default
			      .ParseArguments<Options>(args)
			      .WithParsed(Main);
		}

		private static void Main(Options options)
		{
			ProgramUtils.ProgramNumber = options.Program;

			s_Bootstrap.Start();

			while (true)
			{
				string command = Console.ReadLine();
				if (command == null)
					continue;

				if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
					break;

				ApiConsole.ExecuteCommand(command);
			}

			s_Bootstrap.Stop();
		}
	}
}
#endif
