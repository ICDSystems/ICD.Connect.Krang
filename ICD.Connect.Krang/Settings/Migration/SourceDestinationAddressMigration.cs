using System.Text.RegularExpressions;

namespace ICD.Connect.Krang.Settings.Migration
{
	public static class SourceDestinationAddressMigration
	{
		public static string Migrate(string configXml)
		{
			return Regex.Replace(configXml,
			                     "(?<beginning><(Destination|Source)>(?:.|\\n)*?)<(Input|Output)>(?<addr>\\d+)</\\2>(?<end>(?:.|\\n)*?</\\1>)",
			                     "${beginning}<Address>${addr}</Address>${end}");
		}
	}
}
