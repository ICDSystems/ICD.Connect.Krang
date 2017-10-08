#if !SIMPLSHARP
using System;
using System.Runtime.Loader;
using ICD.Connect.Krang.Core;
using ICD.Connect.API;

namespace ICD.NetStandard.Core
{
	internal static class Program
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
		        ApiConsole.ExecuteCommand(command);
	        }
		}
    }
}
#endif
