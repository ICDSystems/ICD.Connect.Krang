﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Krang.Partitioning;
using ICD.Connect.Krang.Settings;
using ICD.Connect.Panels;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;
using ICD.Connect.Settings.Header;
using ICD.Connect.Themes;

namespace ICD.Connect.Krang.Core
{
	/// <summary>
	/// Settings for the Krang core.
	/// </summary>
	[PublicAPI]
	public sealed class KrangCoreSettings : AbstractSettings, ICoreSettings
	{
		public const string ROOT_ELEMENT = "IcdConfig";

		public const string FACTORY_NAME = "Krang";

		private const string HEADER_ELEMENT = "Header";
		private const string THEMES_ELEMENT = "Themes";
		public const string PANELS_ELEMENT = "Panels";
		public const string PORTS_ELEMENT = "Ports";
		public const string DEVICES_ELEMENT = "Devices";
		public const string ROOMS_ELEMENT = "Rooms";
		private const string ROUTING_ELEMENT = "Routing";
		private const string PARTITIONING_ELEMENT = "Partitioning";
		private const string BROADCAST_ELEMENT = "Broadcast";

		private readonly SettingsCollection m_OriginatorSettings;
		private readonly ConfigurationHeader m_Header;

		#region Properties

		public SettingsCollection OriginatorSettings
		{
			get { return m_OriginatorSettings; }
		}

		/// <summary>
		/// Gets the theme settings.
		/// </summary>
		public SettingsCollection ThemeSettings
		{
			get
			{
				return new SettingsCollection(m_OriginatorSettings.Where(s =>
						s.GetType().IsAssignableTo(typeof (IThemeSettings))));
			}
		}

		/// <summary>
		/// Gets the device settings.
		/// </summary>
		public SettingsCollection DeviceSettings
		{
			get
			{
				return new SettingsCollection(m_OriginatorSettings.Where(s =>
						s.GetType().IsAssignableTo(typeof (IDeviceSettings))));
			}
		}

		/// <summary>
		/// Gets the port settings.
		/// </summary>
		public SettingsCollection PortSettings
		{
			get
			{
				return new SettingsCollection(m_OriginatorSettings.Where(s =>
						s.GetType().IsAssignableTo(typeof (IPortSettings))));
			}
		}

		/// <summary>
		/// Gets the panel settings.
		/// </summary>
		public SettingsCollection PanelSettings
		{
			get
			{
				return new SettingsCollection(m_OriginatorSettings.Where(s =>
						s.GetType().IsAssignableTo(typeof (IPanelDeviceSettings))));
			}
		}

		/// <summary>
		/// Gets the room settings.
		/// </summary>
		public SettingsCollection RoomSettings
		{
			get
			{
				return new SettingsCollection(m_OriginatorSettings.Where(s =>
						s.GetType().IsAssignableTo(typeof (IRoomSettings))));
			}
		}
		
		public RoutingGraphSettings RoutingGraphSettings
		{
			get
			{
				return m_OriginatorSettings.OfType<RoutingGraphSettings>().SingleOrDefault();
			}
		}

		public PartitionManagerSettings PartitionManagerSettings
		{
			get
			{
				return m_OriginatorSettings.OfType<PartitionManagerSettings>().SingleOrDefault();
			}
		}

		/// <summary>
		/// Gets the header info.
		/// </summary>
		public ConfigurationHeader Header
		{
			get { return m_Header; }
		}

		/// <summary>
		/// Gets the broadcast setting.
		/// </summary>
		public bool Broadcast { get; private set; }

		/// <summary>
		/// Gets the xml element.
		/// </summary>
		protected override string Element
		{
			get { return ROOT_ELEMENT; }
		}

		public override string FactoryName
		{
			get { return FACTORY_NAME; }
		}

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(KrangCore); } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangCoreSettings()
		{
			m_OriginatorSettings = new SettingsCollection();
			m_Header = new ConfigurationHeader();

			m_OriginatorSettings.OnItemRemoved += DeviceSettingsOnItemRemoved;
		}

		/// <summary>
		/// Returns the collection of ids that the settings will depend on.
		/// For example, to instantiate an IR Port from settings, the device the physical port
		/// belongs to will need to be instantiated first.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<int> GetDeviceDependencies()
		{
			return m_OriginatorSettings.Select(d => d.Id);
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			new ConfigurationHeader(true).ToXml(writer);

			ThemeSettings.ToXml(writer, THEMES_ELEMENT);
			PanelSettings.ToXml(writer, PANELS_ELEMENT);
			PortSettings.ToXml(writer, PORTS_ELEMENT);
			DeviceSettings.ToXml(writer, DEVICES_ELEMENT);
			RoomSettings.ToXml(writer, ROOMS_ELEMENT);

			RoutingGraphSettings routingGraphSettings = RoutingGraphSettings;
			if (routingGraphSettings != null)
				routingGraphSettings.ToXml(writer);

			PartitionManagerSettings partitionManagerSettings = PartitionManagerSettings;
			if (partitionManagerSettings != null)
				partitionManagerSettings.ToXml(writer);
		}

		#region Protected Methods

		/// <summary>
		/// Parses the xml and applies the properties to the instance.
		/// </summary>
		/// <param name="xml"></param>
		public void ParseXml(string xml)
		{
			ParseXml(this, xml);

			Broadcast = XmlUtils.TryReadChildElementContentAsBoolean(xml, BROADCAST_ELEMENT) ?? false;
			UpdateHeaderFromXml(xml);

			IEnumerable<ISettings> themes = PluginFactory.GetSettingsFromXml(xml, THEMES_ELEMENT);
			IEnumerable<ISettings> panels = PluginFactory.GetSettingsFromXml(xml, PANELS_ELEMENT);
			IEnumerable<ISettings> ports = PluginFactory.GetSettingsFromXml(xml, PORTS_ELEMENT);
			IEnumerable<ISettings> devices = PluginFactory.GetSettingsFromXml(xml, DEVICES_ELEMENT);
			IEnumerable<ISettings> rooms = PluginFactory.GetSettingsFromXml(xml, ROOMS_ELEMENT);

			OriginatorSettings.AddRange(themes);
			OriginatorSettings.AddRange(panels);
			OriginatorSettings.AddRange(ports);
			OriginatorSettings.AddRange(devices);
			OriginatorSettings.AddRange(rooms);

			string child;

			if (XmlUtils.TryGetChildElementAsString(xml, ROUTING_ELEMENT, out child))
				UpdateRoutingFromXml(child);

			if (XmlUtils.TryGetChildElementAsString(xml, PARTITIONING_ELEMENT, out child))
				UpdatePartitioningFromXml(child);
		}

		private void UpdateHeaderFromXml(string xml)
		{
			m_Header.Clear();

			string child;
			if (XmlUtils.TryGetChildElementAsString(xml, HEADER_ELEMENT, out child))
				m_Header.ParseXml(child);
		}

		private void UpdateRoutingFromXml(string xml)
		{
			var routing = new RoutingGraphSettings();
			routing.ParseXml(xml);

			OriginatorSettings.Add(routing);

			//add routing's child originators so they can be accessed by CoreDeviceFactory
			OriginatorSettings.AddRange(routing.ConnectionSettings);
			OriginatorSettings.AddRange(routing.StaticRouteSettings);
			OriginatorSettings.AddRange(routing.SourceSettings);
			OriginatorSettings.AddRange(routing.DestinationSettings);
			OriginatorSettings.AddRange(routing.DestinationGroupSettings);
		}

		private void UpdatePartitioningFromXml(string xml)
		{
			var partitioning = new PartitionManagerSettings();
			partitioning.ParseXml(xml);

			OriginatorSettings.Add(partitioning);

			//add partitioning's child originators so they can be accessed by CoreDeviceFactory
			OriginatorSettings.AddRange(partitioning.PartitionSettings);
		}

		/// <summary>
		/// Called when device settings are removed. We remove any settings that depend on the device.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void DeviceSettingsOnItemRemoved(object sender, EventArgs eventArgs)
		{
			int[] ids = m_OriginatorSettings.Select(s => s.Id).ToArray();

			RemoveSettingsWithBadDeviceDependency(m_OriginatorSettings, ids);
		}

		/// <summary>
		/// Removes settings from the settings collection where a dependency has been lost.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="deviceIds"></param>
		private static void RemoveSettingsWithBadDeviceDependency(ICollection<ISettings> settings,
		                                                          IEnumerable<int> deviceIds)
		{
			IEnumerable<ISettings> remove = settings.ToArray().Where(s => HasBadDeviceDependency(s, deviceIds));
			settings.RemoveAll(remove);
		}

		/// <summary>
		/// Returns true if the settings instance depends on a device that is not in the given device ids.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="deviceIds"></param>
		/// <returns></returns>
		private static bool HasBadDeviceDependency(ISettings settings, IEnumerable<int> deviceIds)
		{
			return settings.GetDeviceDependencies()
			               .ToHashSet()
			               .Subtract(deviceIds.ToHashSet()).Count > 0;
		}

		#endregion
	}
}
