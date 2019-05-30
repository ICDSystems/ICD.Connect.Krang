using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Originators.Simpl;

namespace ICD.Connect.Krang.SPlus.Rooms
{
	[KrangSettings("SimplRoom", typeof(SimplRoom))]
	public sealed class SimplRoomSettings : AbstractRoomSettings, ISimplOriginatorSettings
	{
		private const string CROSSPOINTS_ELEMENT = "Crosspoints";
		private const string CROSSPOINT_ELEMENT = "Crosspoint";
		private const string CROSSPOINT_ID_ELEMENT = "CrosspointId";
		private const string CROSSPOINT_TYPE_ELEMENT = "Type";
		private const string SHORT_NAME_ELEMENT = "ShortName";

		private readonly Dictionary<eCrosspointType, ushort> m_Crosspoints;
		private readonly SafeCriticalSection m_CrosspointsSection;


		public string ShortName { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public SimplRoomSettings()
		{
			m_Crosspoints = new Dictionary<eCrosspointType, ushort>();
			m_CrosspointsSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Gets the crosspoints.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<eCrosspointType, ushort>> GetCrosspoints()
		{
			return m_CrosspointsSection.Execute(() => m_Crosspoints.ToArray());
		}

		/// <summary>
		/// Sets the crosspoints.
		/// </summary>
		/// <param name="crosspoints"></param>
		public void SetCrosspoints(IEnumerable<KeyValuePair<eCrosspointType,ushort>> crosspoints)
		{
			m_CrosspointsSection.Enter();

			try
			{
				m_Crosspoints.Clear();
				m_Crosspoints.AddRange(crosspoints);
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}
		}

		#endregion

		#region Settings

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			XmlUtils.WriteDictToXml(writer, m_Crosspoints, CROSSPOINTS_ELEMENT, CROSSPOINT_ELEMENT, CROSSPOINT_TYPE_ELEMENT,
			                        CROSSPOINT_ID_ELEMENT);

			writer.WriteElementString(SHORT_NAME_ELEMENT, ShortName);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			IEnumerable<KeyValuePair<eCrosspointType,ushort>> crosspoints = ReadCrosspointsFromXml(xml);
			SetCrosspoints(crosspoints);

			ShortName = XmlUtils.TryReadChildElementContentAsString(xml, SHORT_NAME_ELEMENT);
		}

		private static IEnumerable<KeyValuePair<eCrosspointType,ushort>> ReadCrosspointsFromXml(string xml)
		{
			Func<string, eCrosspointType> readKey = s => XmlUtils.ReadElementContentAsEnum<eCrosspointType>(s, true);
			Func<string, ushort> readValue =
				XmlUtils.ReadElementContentAsUShort;

			return XmlUtils.ReadDictFromXml(xml, CROSSPOINTS_ELEMENT, CROSSPOINT_ELEMENT, CROSSPOINT_TYPE_ELEMENT,
			                                CROSSPOINT_ID_ELEMENT, readKey, readValue);
		}

		#endregion
	}
}
