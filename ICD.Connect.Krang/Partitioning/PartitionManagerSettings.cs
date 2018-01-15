using System;
using ICD.Common.Properties;
using ICD.Connect.Partitioning;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Krang.Partitioning
{
	public sealed class PartitionManagerSettings : AbstractPartitionManagerSettings
	{
		private const string FACTORY_NAME = "PartitionManager";

		#region Properties

		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(PartitionManager); } }

		#endregion

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static PartitionManagerSettings FromXml(string xml)
		{
			PartitionManagerSettings output = new PartitionManagerSettings();
			ParseXml(output, xml);
			return output;
		}
	}
}
