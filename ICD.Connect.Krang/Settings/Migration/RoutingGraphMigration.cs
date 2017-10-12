using System;
using System.Linq;
using ICD.Common.Services;
using ICD.Common.Services.Logging;

namespace ICD.Connect.Krang.Settings.Migration
{
	public static class RoutingGraphMigration
	{
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
						   .AddEntry(eSeverity.Notice, "Attempting to migrate old routing config");

			try
			{
				configXml = ReplaceRoutingElement(configXml);
				ServiceProvider.TryGetService<ILoggerService>().AddEntry(eSeverity.Notice, "Successfully migrated config");
				return configXml;
			}
			catch (Exception e)
			{
				ServiceProvider.TryGetService<ILoggerService>()
							   .AddEntry(eSeverity.Error, e, "Failed to migrate config - {0}", e.Message);
				return configXml;
			}
		}

		public static bool HasRoutingGraphOriginator(string configXml)
		{
			return !configXml.Contains("<Routing>");
		}

		private static string ReplaceRoutingElement(string configXml)
		{
			int uniqueId = Enumerable.Range(10000, 10000)
			                         .First(i => !configXml.Contains(i.ToString()));

			return configXml.Replace("<Routing>", string.Format("<Routing id=\"{0}\" type=\"RoutingGraph\">", uniqueId));
		}
	}
}
