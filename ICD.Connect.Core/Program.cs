#if !SIMPLSHARP
using System;
using System.Runtime.Loader;
using ICD.Connect.API;
using ICD.Connect.Krang.Core;

namespace ICD.Connect.Core
{
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

		public static void Main()
		{
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
