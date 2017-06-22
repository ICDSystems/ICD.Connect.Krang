#if !SIMPLSHARP
using System;
using System.Runtime.Loader;
using ICD.SimplSharp.Common.Console;
using ICD.SimplSharp.KrangLib.Core;

namespace ICD.NetStandard.Core
{
	internal class Program
    {
        private static KrangBootstrap s_Bootstrap;
        
	    public static void Main()
        {
            s_Bootstrap = new KrangBootstrap();
            AssemblyLoadContext.Default.Unloading += context => s_Bootstrap.Stop();
            s_Bootstrap.Start();

	        while (true)
	        {
		        string command = Console.ReadLine();
                if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;
		        IcdConsole.ExecuteCommand(command);
	        }
		}
    }
}
#endif
