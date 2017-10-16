using System;
using System.Text.RegularExpressions;
using ICD.Common.Services;
using ICD.Common.Services.Logging;

namespace ICD.Connect.Krang.Settings.Migration
{
	/// <summary>
	/// Migrates the "Connections" element to a "Routing/Connections" element.
	/// </summary>
	public static class ConnectionsRoutingMigration
	{
		private const string REGEX = @"<Connections>[\s\S]*?<\/Connections>";

		/// <summary>
		/// Converts the xml to the newer format if it's in the older format.
		/// </summary>
		/// <param name="configXml"></param>
		/// <returns></returns>
		public static string Migrate(string configXml)
		{
			if (string.IsNullOrEmpty(configXml))
				return configXml;

			ServiceProvider.TryGetService<ILoggerService>()
			               .AddEntry(eSeverity.Notice, "Attempting to migrate old Connections config");

			try
			{
				string output = ConnectionsToRoutingConnections(configXml);
				ServiceProvider.TryGetService<ILoggerService>().AddEntry(eSeverity.Notice, "Successfully migrated config");
				return output;
			}
			catch (Exception e)
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Error, e, "Failed to migrate config - {0}", e.Message);
				return configXml;
			}
		}

		/// <summary>
		/// Returns true if the config has a Routing element.
		/// </summary>
		/// <param name="configXml"></param>
		/// <returns></returns>
		public static bool HasRoutingElement(string configXml)
		{
			return configXml.Contains(@"<Routing") && configXml.Contains(@"</Routing>");
		}

		/// <summary>
		/// Converts the single room config to a multi room config.
		/// </summary>
		/// <param name="configXml"></param>
		/// <returns></returns>
		private static string ConnectionsToRoutingConnections(string configXml)
		{
			Regex re = new Regex(REGEX, RegexOptions.Multiline);

			string connections = re.Match(configXml).Value;
			return re.Replace(configXml, @"<Routing>" + connections + @"</Routing>");
		}
	}
}
