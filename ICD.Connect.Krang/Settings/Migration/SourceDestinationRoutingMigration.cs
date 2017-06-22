using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Krang.Settings.Migration
{
	/// <summary>
	/// This is hacky and ugly, but hopefully we can get rid of it soon.
	/// </summary>
	public static class SourceDestinationRoutingMigration
	{
		private const string ROOM_SOURCES_REGEX =
			@"(?<start>\<Room[\s\S]*)(?<replace>\<Sources\>[\s\S]*\<\/Sources>)(?<end>[\s\S]*Room\>)";

		private const string ROOM_DESTINATIONS_REGEX =
			@"(?<start>\<Room[\s\S]*)(?<replace>\<Destinations\>[\s\S]*\<\/Destinations>)(?<end>[\s\S]*Room\>)";

		private const string ICD_CONFIG_ROUTING_REGEX =
			@"(?<start>\<IcdConfig[\s\S]*\<Routing\>)(?<replace>[\s\S]*)(?<end>\<\/Routing>[\s\S]*IcdConfig\>)";

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
			               .AddEntry(eSeverity.Notice, "Attempting to migrate old Source/Destination config");

			try
			{
				configXml = MoveEndpointsToRoutingElement(configXml);
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

		/// <summary>
		/// Returns true if the config has a source or destination routing element.
		/// </summary>
		/// <param name="configXml"></param>
		/// <returns></returns>
		public static bool HasSourceOrDestinationRoutingElement(string configXml)
		{
			string routing;
			if (!XmlUtils.TryGetChildElementAsString(configXml, "Routing", out routing))
				return false;

			string unused;
			return XmlUtils.TryGetChildElementAsString(routing, "Sources", out unused) ||
			       XmlUtils.TryGetChildElementAsString(routing, "Destinations", out unused);
		}

		/// <summary>
		/// Moves the room sources and destinations to the Routing element.
		/// </summary>
		/// <param name="configXml"></param>
		/// <returns></returns>
		private static string MoveEndpointsToRoutingElement(string configXml)
		{
			bool isMetlifeRoom = configXml.Contains("MetlifeRoom");

			// Get the source and destination elements
			Match sourcesMatch = Regex.Match(configXml, ROOM_SOURCES_REGEX);
			Match destinationsMatch = Regex.Match(configXml, ROOM_DESTINATIONS_REGEX);

			string sources = sourcesMatch.Success ? sourcesMatch.Groups["replace"].Value : string.Empty;
			sources = PopulateIds(sources, "Sources", "Source", isMetlifeRoom ? "MetlifeSource" : "Source").Trim();

			string destinations = destinationsMatch.Success ? destinationsMatch.Groups["replace"].Value : string.Empty;
			destinations =
				PopulateIds(destinations, "Destinations", "Destination", isMetlifeRoom ? "MetlifeDestination" : "Destination")
					.Trim();

			// Add the source and destination elements to the routing element
			Match routingMatch = Regex.Match(configXml, ICD_CONFIG_ROUTING_REGEX);
			string routingContents = routingMatch.Success ? routingMatch.Groups["replace"].Value : string.Empty;

			if (!string.IsNullOrEmpty(sources))
				configXml = Regex.Replace(configXml, ICD_CONFIG_ROUTING_REGEX, "${start}" + routingContents + sources + "${end}");

			if (!string.IsNullOrEmpty(destinations))
			{
				configXml = Regex.Replace(configXml, ICD_CONFIG_ROUTING_REGEX,
				                          "${start}" + routingContents + destinations + "${end}");
			}

			// Replace the sources and destinations in the room with ids
			string sourceIds = GetEndpointsIdsXml(sources, "Sources", "Source");
			string destinationIds = GetEndpointsIdsXml(destinations, "Destinations", "Destination");

			configXml = Regex.Replace(configXml, ROOM_SOURCES_REGEX, "${start}" + sourceIds + "${end}");
			configXml = Regex.Replace(configXml, ROOM_DESTINATIONS_REGEX, "${start}" + destinationIds + "${end}");

			return configXml;
		}

		private static string GetEndpointsIdsXml(string originators, string listElement, string itemElement)
		{
			originators = originators.Trim();
			if (string.IsNullOrEmpty(originators))
				return string.Empty;

			string output = string.Format("<{0}>", listElement);

			foreach (int id in GetIds(originators))
				output += string.Format("<{0}>{1}</{2}>", itemElement, id, itemElement);

			output += string.Format("</{0}>", listElement);

			return output;
		}

		private static IEnumerable<int> GetIds(string originators)
		{
			originators = originators.Trim();
			if (string.IsNullOrEmpty(originators))
				return Enumerable.Empty<int>();

			return XmlUtils.GetChildElementsAsString(originators)
			               .Select(child => XmlUtils.GetAttributeAsInt(child, "id"))
			               .Order();
		}

		/// <summary>
		/// Gives each endpoint element an id.
		/// </summary>
		/// <param name="originators"></param>
		/// <param name="listElement"></param>
		/// <param name="itemElement"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		private static string PopulateIds(string originators, string listElement, string itemElement, string type)
		{
			originators = originators.Trim();
			if (string.IsNullOrEmpty(originators))
				return string.Empty;

			string output = string.Format("<{0}>", listElement);

			foreach (string item in PopulateIds(originators, itemElement, type))
				output += item;

			output += string.Format("</{0}>", listElement);

			return output;
		}

		private static IEnumerable<string> PopulateIds(string originators, string itemElement, string type)
		{
			IcdHashSet<int> usedIds = new IcdHashSet<int>();

			List<string> children = XmlUtils.GetChildElementsAsString(originators).ToList();

			// First pass - get the elements that already have an id
			for (int index = children.Count - 1; index >= 0; index--)
			{
				string child = children[index];
				if (!XmlUtils.HasAttribute(child, "id"))
				{
					child = MoveIdToAttribute(child, itemElement, type);
					children[index] = child;
				}

				if (!XmlUtils.HasAttribute(child, "id"))
					continue;

				int id = XmlUtils.GetAttributeAsInt(child, "id");
				usedIds.Add(id);

				yield return child;
				children.RemoveAt(index);
			}

			// Second pass - give ids to the remaining elements
			foreach (string child in children)
			{
				int id = Enumerable.Range(1, int.MaxValue).First(i => !usedIds.Contains(i));
				usedIds.Add(id);

				yield return
					Regex.Replace(child, "<" + itemElement + ">", string.Format("<{0} id=\"{1}\" type=\"{2}\">", itemElement, id, type))
					;
			}
		}

		private static string MoveIdToAttribute(string originator, string itemElement, string type)
		{
			int? id = XmlUtils.TryReadChildElementContentAsInt(originator, "Id");
			return id == null
				       ? originator
				       : Regex.Replace(originator, "<" + itemElement + ">",
				                       string.Format("<{0} id=\"{1}\" type=\"{2}\">", itemElement, id, type));
		}
	}
}
