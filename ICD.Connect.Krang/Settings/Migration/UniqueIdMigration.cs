using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.Settings.Migration
{
	public static class UniqueIdMigration
	{
		public static string Migrate(string configXml)
		{
			if (string.IsNullOrEmpty(configXml))
				return configXml;

			ServiceProvider.TryGetService<ILoggerService>()
			               .AddEntry(eSeverity.Notice, "Attempting to migrate to unique ids config");

			try
			{
				configXml = UpdateIds(configXml);
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

		public static bool HasUniqueIds(string configXml)
		{
			Dictionary<string, IcdHashSet<int>> originatorIds = GetOriginatorIds(configXml);

			return !originatorIds.Values.Any(v1 => originatorIds.Values.Any(v2 => v1 != v2 && v1.Intersection(v2).Count > 0));
		}

		private static string UpdateIds(string configXml)
		{
			Dictionary<string, IcdHashSet<int>> originatorIds = GetOriginatorIds(configXml);
			IcdHashSet<int> uniqueIds = GetUniqueIds(originatorIds);

			// Build the remap
			IcdHashSet<int> allIds = new IcdHashSet<int>(originatorIds.Values.SelectMany(v => v));
			IcdHashSet<int> usedIds = new IcdHashSet<int>(uniqueIds);
			Dictionary<string, Dictionary<int, int>> remap = new Dictionary<string, Dictionary<int, int>>();

			foreach (string originator in originatorIds.Keys)
			{
				foreach (int id in originatorIds[originator].Where(i => !uniqueIds.Contains(i)))
				{
					int uniqueId = IdUtils.GetNewId(allIds.Concat(usedIds));
					usedIds.Add(uniqueId);

					if (!remap.ContainsKey(originator))
						remap[originator] = new Dictionary<int, int>();
					remap[originator][id] = uniqueId;
				}
			}

			// Apply the remap
			foreach (string originator in remap.Keys)
			{
				foreach (KeyValuePair<int, int> kvp in remap[originator])
				{
					int oldId = kvp.Key;
					int newId = kvp.Value;

					configXml = configXml.Replace(string.Format("<{0}>{1}</{0}>", originator, oldId),
					                              string.Format("<{0}>{1}</{0}>", originator, newId));

					configXml = configXml.Replace(string.Format("<{0} id=\"{1}\"", originator, oldId),
					                              string.Format("<{0} id=\"{1}\"", originator, newId));

					// Edge case
					if (originator == "Device")
					{
						configXml = configXml.Replace(string.Format("<Source{0}>{1}</Source{0}>", originator, oldId),
						                              string.Format("<Source{0}>{1}</Source{0}>", originator, newId));

						configXml = configXml.Replace(string.Format("<Destination{0}>{1}</Destination{0}>", originator, oldId),
						                              string.Format("<Destination{0}>{1}</Destination{0}>", originator, newId));
					}
				}
			}

			return configXml;
		}

		private static IcdHashSet<int> GetUniqueIds(Dictionary<string, IcdHashSet<int>> originatorIds)
		{
			Dictionary<int, int> idOccurences = new Dictionary<int, int>();

			foreach (string originator in originatorIds.Keys)
			{
				foreach (int id in originatorIds[originator])
					idOccurences[id] = idOccurences.GetDefault(id, 0) + 1;
			}

			return idOccurences.Keys.Where(k => idOccurences[k] == 1).ToHashSet();
		}

		private static Dictionary<string, IcdHashSet<int>> GetOriginatorIds(string configXml)
		{
			Dictionary<string, IcdHashSet<int>> output = new Dictionary<string, IcdHashSet<int>>();

			XmlUtils.Recurse(configXml, args =>
			                            {
				                            if (!XmlUtils.HasAttribute(args.Outer, "id"))
					                            return;

				                            string name = XmlUtils.ReadElementName(args.Outer);

											// Edge case
				                            if (name == "IcdConfig")
					                            return;

				                            int id = XmlUtils.GetAttributeAsInt(args.Outer, "id");

											if (!output.ContainsKey(name))
												output[name] = new IcdHashSet<int>();

				                            output[name].Add(id);
			                            });

			return output;
		}
	}
}
