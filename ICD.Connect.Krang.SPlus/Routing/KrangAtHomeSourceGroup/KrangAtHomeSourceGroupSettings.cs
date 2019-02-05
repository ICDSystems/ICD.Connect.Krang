using System.Collections.Generic;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Xml;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup
{
	public sealed class KrangAtHomeSourceGroupSettings : AbstractSettings
	{

		private IcdOrderedDictionary<int, List<IKrangAtHomeSource>> m_Sources;

		public IcdOrderedDictionary<int, List<IKrangAtHomeSource>> Sources
		{
			get { return m_Sources; }
			set { m_Sources = value; }
		}


		public KrangAtHomeSourceGroupSettings()
		{
			m_Sources = new IcdOrderedDictionary<int, List<IKrangAtHomeSource>>();
		}


		#region XML

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			//todo: Parse XML
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			//todo: Write XML
		}

		#endregion

	}
}