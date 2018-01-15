using System;
using ICD.Common.Properties;
using ICD.Connect.Routing;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Krang.Routing
{
	public sealed class RoutingGraphSettings : AbstractRoutingGraphSettings
	{
		private const string FACTORY_NAME = "RoutingGraph";

		#region Properties

		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(RoutingGraph); } }

		#endregion

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static RoutingGraphSettings FromXml(string xml)
		{
			RoutingGraphSettings output = new RoutingGraphSettings();
			ParseXml(output, xml);
			return output;
		}
	}
}
