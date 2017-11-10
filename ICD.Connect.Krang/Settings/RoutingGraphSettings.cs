using System;
using System.Collections.Generic;
using ICD.Common.Utils.Xml;
using ICD.Connect.Krang.Routing;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.Settings
{
	public sealed class RoutingGraphSettings : AbstractSettings
	{
		private const string ELEMENT_NAME = "Routing";
		private const string FACTORY_NAME = "RoutingGraph";
		private const string CONNECTIONS_ELEMENT = "Connections";
		private const string STATIC_ROUTES_ELEMENT = "StaticRoutes";
		private const string SOURCES_ELEMENT = "Sources";
		private const string DESTINATIONS_ELEMENT = "Destinations";
		private const string DESTINATION_GROUPS_ELEMENT = "DestinationGroups";

		private readonly SettingsCollection m_ConnectionSettings;
		private readonly SettingsCollection m_StaticRouteSettings;
		private readonly SettingsCollection m_SourceSettings;
		private readonly SettingsCollection m_DestinationSettings;
		private readonly SettingsCollection m_DestinationGroupSettings;

		#region Properties

		public SettingsCollection ConnectionSettings { get { return m_ConnectionSettings; } }
		public SettingsCollection StaticRouteSettings { get { return m_StaticRouteSettings; } }
		public SettingsCollection SourceSettings { get { return m_SourceSettings; } }
		public SettingsCollection DestinationSettings { get { return m_DestinationSettings; } }
		public SettingsCollection DestinationGroupSettings { get { return m_DestinationGroupSettings; } }

		protected override string Element { get { return ELEMENT_NAME; } }

		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(RoutingGraph); } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public RoutingGraphSettings()
		{
			m_ConnectionSettings = new SettingsCollection();
			m_StaticRouteSettings = new SettingsCollection();
			m_SourceSettings = new SettingsCollection();
			m_DestinationSettings = new SettingsCollection();
			m_DestinationGroupSettings = new SettingsCollection();
		}

		#region Methods

		/// <summary>
		/// Writes the routing settings to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			m_ConnectionSettings.ToXml(writer, CONNECTIONS_ELEMENT);
			m_StaticRouteSettings.ToXml(writer, STATIC_ROUTES_ELEMENT);
			m_SourceSettings.ToXml(writer, SOURCES_ELEMENT);
			m_DestinationSettings.ToXml(writer, DESTINATIONS_ELEMENT);
			m_DestinationGroupSettings.ToXml(writer, DESTINATION_GROUPS_ELEMENT);
		}

		public void ParseXml(string xml)
		{
			IEnumerable<ISettings> connections = PluginFactory.GetSettingsFromXml(xml, CONNECTIONS_ELEMENT);
			IEnumerable<ISettings> staticRoutes = PluginFactory.GetSettingsFromXml(xml, STATIC_ROUTES_ELEMENT);
			IEnumerable<ISettings> sources = PluginFactory.GetSettingsFromXml(xml, SOURCES_ELEMENT);
			IEnumerable<ISettings> destinations = PluginFactory.GetSettingsFromXml(xml, DESTINATIONS_ELEMENT);
			IEnumerable<ISettings> destinationGroups = PluginFactory.GetSettingsFromXml(xml, DESTINATION_GROUPS_ELEMENT);

			m_ConnectionSettings.SetRange(connections);
			m_StaticRouteSettings.SetRange(staticRoutes);
			m_SourceSettings.SetRange(sources);
			m_DestinationSettings.SetRange(destinations);
			m_DestinationGroupSettings.SetRange(destinationGroups);

			ParseXml(this, xml);
		}

		public void Clear()
		{
			m_ConnectionSettings.Clear();
			m_StaticRouteSettings.Clear();
			m_SourceSettings.Clear();
			m_DestinationSettings.Clear();
			m_DestinationGroupSettings.Clear();
		}

		#endregion
	}
}
