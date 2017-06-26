#if SIMPLSHARP
using System;
using System.Linq;
using Crestron.SimplSharp.WebScripting;
using ICD.Connect.API.Nodes;
using ICD.Common.Utils;

namespace ICD.Connect.Core
{
	public static class ConsoleCommandServer
	{
		private static readonly HttpCwsServer s_Server;
		private static IConsoleNodeBase s_Root;

		static ConsoleCommandServer()
		{
			s_Server = new HttpCwsServer("/icd");
			s_Server.ReceivedRequestEvent += ServerOnReceivedRequestEvent;
			s_Server.Register();
		}

		private static void ServerOnReceivedRequestEvent(object sender, HttpCwsRequestEventArgs args)
		{
			string[] split =
				args.Context.Request.Path.Split('/')
				    .Where(s => !s.Equals("icd", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(s))
				    .ToArray();
			string response = s_Root.ExecuteConsoleCommand(split);
			response = s_Server.HtmlEncode(response).Replace(IcdEnvironment.NewLine, "<br/>");
			args.Context.Response.Write("<style>body{font-family:monospace;}</style><pre>", false);
			args.Context.Response.Write(response, false);
			args.Context.Response.Write("</pre>", true);
		}

		public static void RegisterChild(IConsoleNodeBase node)
		{
			s_Root = node;
		}

		public static void Dispose()
		{
			s_Server.Dispose();
		}
	}
}
#endif