using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Xml;
using ICD.Connect.Krang.Core;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Header;
#if SIMPLSHARP
using Crestron.SimplSharp.CrestronXml;
#else
using System.Xml;
#endif

namespace ICD.Connect.Krang.Settings.Migration
{
	/// <summary>
	/// Temporary class for converting an older, single room config to the new multi-room, Core config.
	/// </summary>
	public static class LegacySettingsMigration
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
			               .AddEntry(eSeverity.Notice, "Attempting to migrate old single-room config");

			try
			{
				string output = SingleRoomToMultiRoom(configXml);
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
		/// Returns true if the config represents a single room.
		/// </summary>
		/// <param name="configXml"></param>
		/// <returns></returns>
		public static bool IsSingleRoom(string configXml)
		{
			if (configXml == null)
				throw new ArgumentNullException("configXml");

			using (IcdXmlReader reader = new IcdXmlReader(configXml))
			{
				reader.SkipToNextElement();
				return reader.NodeType == XmlNodeType.Element && reader.Name == "Room";
			}
		}

		/// <summary>
		/// Converts the single room config to a multi room config.
		/// </summary>
		/// <param name="configXml"></param>
		/// <returns></returns>
		private static string SingleRoomToMultiRoom(string configXml)
		{
			StringBuilder output = new StringBuilder();

			using (IcdTextWriter textWriter = new IcdEncodingStringWriter(output, Encoding.UTF8))
			{
				using (IcdXmlTextWriter writer = new IcdXmlTextWriter(textWriter))
					SingleRoomToMultiRoom(writer, configXml);
			}

			return output.ToString();
		}

		/// <summary>
		/// Converts the single room config to a multi room config.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="configXml"></param>
		/// <returns></returns>
		private static void SingleRoomToMultiRoom(IcdXmlTextWriter writer, string configXml)
		{
			writer.WriteStartElement(KrangCoreSettings.ROOT_ELEMENT);
			{
				writer.WriteAttributeString(AbstractSettings.ID_ATTRIBUTE, "1");
				writer.WriteAttributeString(AbstractSettings.TYPE_ATTRIBUTE, KrangCoreSettings.FACTORY_NAME);

				// Name of the core
				writer.WriteElementString(AbstractSettings.NAME_ELEMENT, "IcdCore");

				// Header info
				new ConfigurationHeader(true).ToXml(writer);

				// The meat
				CopyXml(writer, configXml, KrangCoreSettings.PORTS_ELEMENT);
				CopyXml(writer, configXml, KrangCoreSettings.DEVICES_ELEMENT);
				CopyXml(writer, configXml, KrangCoreSettings.PANELS_ELEMENT);

				// Routes got renamed
				CopyRoutesXml(writer, configXml);

				// Create the new room
				WriteRooms(writer, configXml);
			}
			writer.WriteEndElement();
		}

		private static void CopyXml(IcdXmlTextWriter writer, string configXml, string element)
		{
			string child = XmlUtils.GetChildElementAsString(configXml, element);
			writer.WriteRaw(child);
		}

		private static void CopyRoutesXml(IcdXmlTextWriter writer, string configXml)
		{
			string child = XmlUtils.GetChildElementAsString(configXml, "Routes");

			// Routes got renamed to connections
			child = child.Replace("Route", "Connection");

			writer.WriteRaw(child);
		}

		private static void WriteRooms(IcdXmlTextWriter writer, string configXml)
		{
			writer.WriteStartElement(KrangCoreSettings.ROOMS_ELEMENT);
			{
				writer.WriteStartElement(AbstractRoomSettings.ROOM_ELEMENT);
				{
					IEnumerable<IcdXmlAttribute> attributes = XmlUtils.GetAttributes(configXml);
					foreach (IcdXmlAttribute attribute in attributes)
						writer.WriteAttributeString(attribute.Name, attribute.Value);

					string devicesXml = null;
					string sourcesXml = null;

					foreach (string child in XmlUtils.GetChildElementsAsString(configXml))
					{
						string name = XmlUtils.ReadElementName(child);

						switch (name)
						{
							case KrangCoreSettings.PORTS_ELEMENT:
								WriteIds(writer, child, KrangCoreSettings.PORTS_ELEMENT, AbstractPortSettings.PORT_ELEMENT);
								break;
							case KrangCoreSettings.DEVICES_ELEMENT:
								devicesXml = child;
								WriteIds(writer, child, KrangCoreSettings.DEVICES_ELEMENT, AbstractDeviceSettings.DEVICE_ELEMENT);
								break;
							case KrangCoreSettings.PANELS_ELEMENT:
								WriteIds(writer, child, KrangCoreSettings.PANELS_ELEMENT, AbstractPanelDeviceSettings.PANEL_ELEMENT);
								break;

							case "Sources":
								sourcesXml = child;
								break;

							case "Routes":
								continue;

							default:
								writer.WriteRaw(child);
								break;
						}
					}

					WriteSources(writer, sourcesXml, devicesXml);
				}
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// Takes the full Devices/Ports/Panels xml element and writes out the ids in the same structure.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="list"></param>
		/// <param name="listElement"></param>
		/// <param name="itemElement"></param>
		private static void WriteIds(IcdXmlTextWriter writer, string list, string listElement, string itemElement)
		{
			writer.WriteStartElement(listElement);
			{
				foreach (string child in XmlUtils.GetChildElementsAsString(list))
				{
					int id = XmlUtils.GetAttributeAsInt(child, "id");
					writer.WriteElementString(itemElement, IcdXmlConvert.ToString(id));
				}
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// Old sources were simply listed by id. Now sources include addresses, icon, UI location, etc.
		/// This is kinda ugly because we need to take the ids xml and compare it against the devices xml.
		/// 
		/// This is even more ugly because sources are metlife specific, and we don't reference that project here.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="sourcesXml"></param>
		/// <param name="devicesXml"></param>
		private static void WriteSources(IcdXmlTextWriter writer, string sourcesXml, string devicesXml)
		{
			if (devicesXml == null)
			{
				writer.WriteRaw(sourcesXml);
				return;
			}

			// Build a map of source id to device type.
			const string deviceRegex = @"<Device id=\""(\d+)\"" type=\""(\w+)\""";
			Regex re = new Regex(deviceRegex, RegexOptions.Multiline);

			Dictionary<int, string> idTypeMap = new Dictionary<int, string>();
			re.Matches(devicesXml)
			  .Cast<Match>()
			  .ForEach(match =>
			           {
				           int deviceId = int.Parse(match.Groups[1].Value);
				           string type = match.Groups[2].Value;
				           idTypeMap.Add(deviceId, type);
			           });

			// Get the source device ids.
			IcdHashSet<int> deviceIds = XmlUtils.GetChildElementsAsString(sourcesXml)
			                                    .Select(c => XmlUtils.ReadElementContentAsInt(c))
			                                    .Order()
			                                    .ToHashSet();

			// Add the tv tuner to the list of device ids.
			if (idTypeMap.ContainsValue("IrTvTuner"))
				deviceIds.Add(idTypeMap.GetKey("IrTvTuner"));

			/*
			  <Sources>
				<Source>
				  <Device>6</Device>
				  <Output>1</Output>
				  <SourceType>Laptop</SourceType>
				  <SourceFlags>Share</SourceFlags>
				  <EnableWhenNotTransmitting>false</EnableWhenNotTransmitting>
				</Source>
			  </Sources>
			 */

			writer.WriteStartElement("Sources");
			{
				foreach (int deviceId in deviceIds.Order())
				{
					// Assuming output 1 on all devices.
					const int output = 1;

					string name = null;
					string sourceType = "Laptop";
					string sourceFlags = "Share";
					bool enableWhenNotTransmitting = false;

					string deviceType;
					idTypeMap.TryGetValue(deviceId, out deviceType);

					switch (deviceType)
					{
						case "BarcoClickshare":
							sourceType = "Wireless";
							enableWhenNotTransmitting = true;
							break;

						case "IrTvTuner":
							name = "TV"; // I.e. "Watch TV" instead of "Watch TV Tuner"
							sourceType = "CableBox";
							sourceFlags = "MainNav";
							break;
					}

					writer.WriteStartElement("Source");
					{
						writer.WriteElementString("Device", IcdXmlConvert.ToString(deviceId));
						writer.WriteElementString("Output", IcdXmlConvert.ToString(output));
						writer.WriteElementString("Name", name);
						writer.WriteElementString("SourceType", sourceType);
						writer.WriteElementString("SourceFlags", sourceFlags);
						writer.WriteElementString("EnableWhenNotTransmitting", IcdXmlConvert.ToString(enableWhenNotTransmitting));
					}
					writer.WriteEndElement();
				}
			}
			writer.WriteEndElement();
		}
	}
}
