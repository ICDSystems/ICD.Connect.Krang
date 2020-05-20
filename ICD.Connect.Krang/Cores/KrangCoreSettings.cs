﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.VolumePoints;
using ICD.Connect.Calendaring.CalendarPoints;
using ICD.Connect.Conferencing.ConferencePoints;
using ICD.Connect.Devices;
using ICD.Connect.Partitioning.PartitionManagers;
using ICD.Connect.Partitioning.RoomGroups;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Cores;
using ICD.Connect.Settings.Utils;
using ICD.Connect.Telemetry.Services;
using ICD.Connect.Themes;

namespace ICD.Connect.Krang.Cores
{
	/// <summary>
	/// Settings for the Krang core.
	/// </summary>
	[KrangSettings("Krang", typeof(KrangCore))]
	public sealed class KrangCoreSettings : AbstractCoreSettings
	{
		private const string THEMES_ELEMENT = "Themes";
		private const string THEME_ELEMENT = "Theme";
		[Obsolete] private const string PANELS_ELEMENT = "Panels";
		private const string PORTS_ELEMENT = "Ports";
		private const string PORT_ELEMENT = "Port";
		private const string DEVICES_ELEMENT = "Devices";
		private const string DEVICE_ELEMENT = "Device";
		private const string ROOMS_ELEMENT = "Rooms";
		private const string ROOM_ELEMENT = "Room";
		private const string VOLUME_POINTS_ELEMENT = "VolumePoints";
		private const string VOLUME_POINT_ELEMENT = "VolumePoint";
		private const string CONFERENCE_POINTS_ELEMENT = "ConferencePoints";
		private const string CONFERENCE_POINT_ELEMENT = "ConferencePoint";
		private const string CALENDAR_POINTS_ELEMENT = "CalendarPoints";
		private const string CALENDAR_POINT_ELEMENT = "CalendarPoint";

		private const string ROOM_GROUPS_ELEMENT = "RoomGroups";
		private const string ROOM_GROUP_ELEMENT = "RoomGroup";

		private const string ROUTING_ELEMENT = "Routing";
		private const string PARTITIONING_ELEMENT = "Partitioning";
		private const string INTER_CORE_ELEMENT = "InterCore";
		private const string TELEMETRY_ELEMENT = "Telemetry";

		#region Properties

		private RoutingGraphSettings RoutingGraphSettings
		{
			get { return OriginatorSettings.OfType<RoutingGraphSettings>().SingleOrDefault(); }
		}

		private PartitionManagerSettings PartitionManagerSettings
		{
			get { return OriginatorSettings.OfType<PartitionManagerSettings>().SingleOrDefault(); }
		}

		/// <summary>
		/// Gets the broadcasting configuration.
		/// </summary>
		public InterCoreServiceSettings InterCoreServiceSettings
		{
			get { return OriginatorSettings.OfType<InterCoreServiceSettings>().SingleOrDefault(); }
		}

		/// <summary>
		/// Gets the telemetry configuration.
		/// </summary>
		public TelemetryServiceSettings TelemetrySettings
		{
			get { return OriginatorSettings.OfType<TelemetryServiceSettings>().SingleOrDefault(); }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			InterCoreServiceSettings interCoreServiceSettings = InterCoreServiceSettings;
			if (interCoreServiceSettings != null)
				interCoreServiceSettings.ToXml(writer, INTER_CORE_ELEMENT);

			TelemetryServiceSettings telemetrySettings = TelemetrySettings;
			if (telemetrySettings != null)
				telemetrySettings.ToXml(writer, TELEMETRY_ELEMENT);

			GetSettings<IThemeSettings>().ToXml(writer, THEMES_ELEMENT, THEME_ELEMENT);
			GetSettings<IPortSettings>().ToXml(writer, PORTS_ELEMENT, PORT_ELEMENT);
			GetSettings<IDeviceSettings>().ToXml(writer, DEVICES_ELEMENT, DEVICE_ELEMENT);
			GetSettings<IRoomSettings>().ToXml(writer, ROOMS_ELEMENT, ROOM_ELEMENT);
			GetSettings<IVolumePointSettings>().ToXml(writer, VOLUME_POINTS_ELEMENT, VOLUME_POINT_ELEMENT);
			GetSettings<IConferencePointSettings>().ToXml(writer, CONFERENCE_POINTS_ELEMENT, CONFERENCE_POINT_ELEMENT);
			GetSettings<ICalendarPointSettings>().ToXml(writer, CALENDAR_POINTS_ELEMENT, CALENDAR_POINT_ELEMENT);
			GetSettings<IRoomGroupSettings>().ToXml(writer, ROOM_GROUPS_ELEMENT, ROOM_GROUP_ELEMENT);

			RoutingGraphSettings routingGraphSettings = RoutingGraphSettings;
			if (routingGraphSettings != null)
				routingGraphSettings.ToXml(writer, ROUTING_ELEMENT);

			PartitionManagerSettings partitionManagerSettings = PartitionManagerSettings;
			if (partitionManagerSettings != null)
				partitionManagerSettings.ToXml(writer, PARTITIONING_ELEMENT);
		}

		/// <summary>
		/// Parses the xml and applies the properties to the instance.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			string child;

			XmlUtils.TryGetChildElementAsString(xml, INTER_CORE_ELEMENT, out child);
			UpdateBroadcastFromXml(child);

			XmlUtils.TryGetChildElementAsString(xml, TELEMETRY_ELEMENT, out child);
			UpdateTelemetryFromXml(child);

			IEnumerable<ISettings> themes = PluginFactory.GetSettingsFromXml(xml, THEMES_ELEMENT);
// ReSharper disable CSharpWarnings::CS0612
			// Backwards compatibility
			IEnumerable<ISettings> panels = PluginFactory.GetSettingsFromXml(xml, PANELS_ELEMENT);
// ReSharper restore CSharpWarnings::CS0612
			IEnumerable<ISettings> ports = PluginFactory.GetSettingsFromXml(xml, PORTS_ELEMENT);
			IEnumerable<ISettings> devices = PluginFactory.GetSettingsFromXml(xml, DEVICES_ELEMENT);
			IEnumerable<ISettings> rooms = PluginFactory.GetSettingsFromXml(xml, ROOMS_ELEMENT);
			IEnumerable<ISettings> volumePoints = PluginFactory.GetSettingsFromXml(xml, VOLUME_POINTS_ELEMENT);
			IEnumerable<ISettings> conferencePoints = PluginFactory.GetSettingsFromXml(xml, CONFERENCE_POINTS_ELEMENT);
			IEnumerable<ISettings> calendarPoints = PluginFactory.GetSettingsFromXml(xml, CALENDAR_POINTS_ELEMENT);
			IEnumerable<ISettings> roomGroups = PluginFactory.GetSettingsFromXml(xml, ROOM_GROUPS_ELEMENT);

			IEnumerable<ISettings> concat =
				themes.Concat(panels)
					  .Concat(ports)
					  .Concat(devices)
					  .Concat(rooms)
					  .Concat(volumePoints)
					  .Concat(conferencePoints)
					  .Concat(calendarPoints)
					  .Concat(roomGroups);

			AddSettingsSkipDuplicateIds(concat);

			XmlUtils.TryGetChildElementAsString(xml, ROUTING_ELEMENT, out child);
			UpdateRoutingFromXml(child);

			XmlUtils.TryGetChildElementAsString(xml, PARTITIONING_ELEMENT, out child);
			UpdatePartitioningFromXml(child);
		}

		#endregion

		#region Private Methods

		private SettingsCollection GetSettings<T>()
			where T : ISettings
		{
			return new SettingsCollection(OriginatorSettings.Where(s => s is T));
		}

		/// <summary>
		/// Adds the given settings instances to the core settings collection.
		/// Logs and skips any items with duplicate ids.
		/// </summary>
		/// <param name="settings"></param>
		private void AddSettingsSkipDuplicateIds(IEnumerable<ISettings> settings)
		{
			if (settings == null)
				throw new ArgumentNullException("settings");

			foreach (ISettings item in settings)
				AddSettingsSkipDuplicateId(item);
		}

		/// <summary>
		/// Adds the given settings instance to the core settings collection.
		/// Logs and skips any item with duplicate id.
		/// </summary>
		/// <param name="settings"></param>
		private bool AddSettingsSkipDuplicateId(ISettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException("settings");

			if (OriginatorSettings.Add(settings))
				return true;

			Logger.AddEntry(eSeverity.Error, "{0} failed to add {1} - Duplicate ID", this, settings);
			return false;
		}

		/// <summary>
		/// Adds the collection settings instances to the core settings collection.
		/// Logs and skips any item with duplicate id.
		/// Removes items with duplicate ids from the given collection.
		/// </summary>
		/// <param name="collection"></param>
		private void AddSettingsRemoveOnDuplicateId(ICollection<ISettings> collection)
		{
			foreach (ISettings item in collection.ToArray(collection.Count))
			{
				if (!AddSettingsSkipDuplicateId(item))
					collection.Remove(item);
			}
		}

		private void UpdateBroadcastFromXml(string xml)
		{
			InterCoreServiceSettings interCoreService = new InterCoreServiceSettings
			{
				Id = IdUtils.GetNewId(OriginatorSettings.Select(s => s.Id), IdUtils.ID_INTER_CORE)
			};

			if (xml != null)
				interCoreService.ParseXml(xml);

			if (!AddSettingsSkipDuplicateId(interCoreService))
				return;

			// Add broadcasts child originators so they can be accessed by CoreDeviceFactory
			AddSettingsRemoveOnDuplicateId(interCoreService.ProviderSettings);
		}

		private void UpdateTelemetryFromXml(string xml)
		{
			TelemetryServiceSettings telemetry = new TelemetryServiceSettings
			{
				Id = IdUtils.GetNewId(OriginatorSettings.Select(s => s.Id), IdUtils.ID_TELEMETRY)
			};

			if (xml != null)
				telemetry.ParseXml(xml);

			if (!AddSettingsSkipDuplicateId(telemetry))
				return;

			// Add telemetrys child originators so they can be accessed by CoreDeviceFactory
			AddSettingsRemoveOnDuplicateId(telemetry.ProviderSettings);
		}

		private void UpdateRoutingFromXml(string xml)
		{
			RoutingGraphSettings routing = new RoutingGraphSettings
			{
				Id = IdUtils.GetNewId(OriginatorSettings.Select(s => s.Id), IdUtils.ID_ROUTING_GRAPH)
			};

			if (xml != null)
				routing.ParseXml(xml);

			if (!AddSettingsSkipDuplicateId(routing))
				return;

			// Add routings child originators so they can be accessed by CoreDeviceFactory
			AddSettingsRemoveOnDuplicateId(routing.ConnectionSettings);
			AddSettingsRemoveOnDuplicateId(routing.StaticRouteSettings);
			AddSettingsRemoveOnDuplicateId(routing.SourceSettings);
			AddSettingsRemoveOnDuplicateId(routing.DestinationSettings);
			AddSettingsRemoveOnDuplicateId(routing.SourceGroupSettings);
			AddSettingsRemoveOnDuplicateId(routing.DestinationGroupSettings);
		}

		private void UpdatePartitioningFromXml(string xml)
		{
			PartitionManagerSettings partitioning = new PartitionManagerSettings
			{
				Id = IdUtils.GetNewId(OriginatorSettings.Select(s => s.Id), IdUtils.ID_PARTITION_MANAGER)
			};

			if (xml != null)
				partitioning.ParseXml(xml);

			if (!AddSettingsSkipDuplicateId(partitioning))
				return;

			// Add partitionings child originators so they can be accessed by CoreDeviceFactory
			AddSettingsRemoveOnDuplicateId(partitioning.CellSettings);
			AddSettingsRemoveOnDuplicateId(partitioning.PartitionSettings);
		}

		/// <summary>
		/// Called when device settings are removed. We remove any settings that depend on the device.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		protected override void OriginatorSettingsOnItemRemoved(object sender, GenericEventArgs<ISettings> eventArgs)
		{
			base.OriginatorSettingsOnItemRemoved(sender, eventArgs);

			// Remove from broadcast
			InterCoreServiceSettings interCoreServiceSettings = InterCoreServiceSettings;
			if (interCoreServiceSettings != null)
				RemoveDependentSettings(interCoreServiceSettings.ProviderSettings, eventArgs.Data);

			// Remove from telemetry
			TelemetryServiceSettings telemetrySettings = TelemetrySettings;
			if (telemetrySettings != null)
				RemoveDependentSettings(telemetrySettings.ProviderSettings, eventArgs.Data);

			// Remove from the routing graph
			RoutingGraphSettings routingGraphSettings = RoutingGraphSettings;
			if (routingGraphSettings != null)
			{
				RemoveDependentSettings(routingGraphSettings.ConnectionSettings, eventArgs.Data);
				RemoveDependentSettings(routingGraphSettings.DestinationSettings, eventArgs.Data);
				RemoveDependentSettings(routingGraphSettings.SourceSettings, eventArgs.Data);
				RemoveDependentSettings(routingGraphSettings.DestinationGroupSettings, eventArgs.Data);
				RemoveDependentSettings(routingGraphSettings.SourceGroupSettings, eventArgs.Data);
				RemoveDependentSettings(routingGraphSettings.StaticRouteSettings, eventArgs.Data);
			}

			// Remove from the partition manager
			PartitionManagerSettings partitionManagerSettings = PartitionManagerSettings;
			if (partitionManagerSettings != null)
			{
				RemoveDependentSettings(partitionManagerSettings.CellSettings, eventArgs.Data);
				RemoveDependentSettings(partitionManagerSettings.PartitionSettings, eventArgs.Data);
			}

			// Remove from the rooms
			foreach (IRoomSettings roomSettings in OriginatorSettings.OfType<IRoomSettings>())
			{
				roomSettings.Devices.Remove(eventArgs.Data.Id);
				roomSettings.Ports.Remove(eventArgs.Data.Id);
				roomSettings.Sources.Remove(eventArgs.Data.Id);
				roomSettings.Destinations.Remove(eventArgs.Data.Id);
				roomSettings.VolumePoints.Remove(eventArgs.Data.Id);
			}
		}

		#endregion
	}
}
