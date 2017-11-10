using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Xml;
using ICD.Connect.Krang.Settings;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.Partitioning
{
    public sealed class PartitionManagerSettings : AbstractSettings
    {
        private const string ELEMENT_NAME = "Partitioning";

        private const string FACTORY_NAME = "PartitionManager";
        private const string PARTITIONS_ELEMENT = "Partitions";

        private readonly SettingsCollection m_PartitionSettings;

        #region Properties

        public SettingsCollection PartitionSettings { get { return m_PartitionSettings; } }

        protected override string Element { get { return ELEMENT_NAME; } }

        public override string FactoryName { get { return FACTORY_NAME; } }

        /// <summary>
        /// Gets the type of the originator for this settings instance.
        /// </summary>
        public override Type OriginatorType { get { return typeof(PartitionManager); } }

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public PartitionManagerSettings()
        {
            m_PartitionSettings = new SettingsCollection();
        }

        #region Methods

        /// <summary>
        /// Writes the routing settings to xml.
        /// </summary>
        /// <param name="writer"></param>
        protected override void WriteElements(IcdXmlTextWriter writer)
        {
            base.WriteElements(writer);

            m_PartitionSettings.ToXml(writer, PARTITIONS_ELEMENT);
        }

        public void ParseXml(string xml)
        {
            IEnumerable<ISettings> partitions =
                PluginFactory.GetSettingsFromXml(xml, PARTITIONS_ELEMENT);

            m_PartitionSettings.SetRange(partitions);

            ParseXml(this, xml);
        }

        public void Clear()
        {
            m_PartitionSettings.Clear();
        }

        /// <summary>
        /// Returns true if the settings depend on a device with the given ID.
        /// For example, to instantiate an IR Port from settings, the device the physical port
        /// belongs to will need to be instantiated first.
        /// </summary>
        /// <returns></returns>
        public override bool HasDeviceDependency(int id)
        {
            return m_PartitionSettings.Select(s => s.Id).Contains(id);
        }

        /// <summary>
        /// Returns the count from the collection of ids that the settings depends on.
        /// </summary>
        public override int DependencyCount
        {
            get { return m_PartitionSettings.Count; }
        }
        #endregion
    }
}