using System;
using System.Linq;
using System.Text.RegularExpressions;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Krang.Settings.Migration
{
	public static class SourceDestinationAddressMigration
	{
		public static string Migrate(string configXml)
		{
			if (string.IsNullOrEmpty(configXml))
				return configXml;

			ServiceProvider.TryGetService<ILoggerService>()
			               .AddEntry(eSeverity.Notice, "Attempting to migrate Input/Output config");

			try
			{
				configXml = Regex.Replace(configXml,
				                          "(?<beginning><(Destination|Source)>(?:.|\\n)*?)<(Input|Output)>(?<addr>\\d+)</\\2>(?<end>(?:.|\\n)*?</\\1>)",
				                          "${beginning}<Address>${addr}</Address>${end}");
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

		public static bool HasOutputOrInput(string configXml)
		{
			IcdHashSet<string> sourceDestinationElements = new IcdHashSet<string>();

			XmlUtils.Recurse(configXml, args =>
			                            {
				                            string name = XmlUtils.ReadElementName(args.Outer);
				                            if (name == "Source" || name == "Destination")
					                            sourceDestinationElements.Add(args.Outer);
			                            });

			return sourceDestinationElements.Any(e => XmlUtils.GetChildElementsAsString(e)
			                                                  .Any(c =>
			                                                       {
				                                                       string name = XmlUtils.ReadElementName(c);
				                                                       return name == "Input" || name == "Output";
			                                                       }));
		}
	}
}
