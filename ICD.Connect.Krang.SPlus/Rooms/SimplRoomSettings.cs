using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Simpl;

namespace ICD.Connect.Krang.SPlus.Rooms
{
	[KrangSettings("SimplRoom", typeof(SimplRoom))]
	public sealed class SimplRoomSettings : AbstractRoomSettings, ISimplOriginatorSettings
	{
		private const string CROSSPOINTS_ELEMENT = "Crosspoints";
		private const string CROSSPOINT_ELEMENT = "Crosspoint";
		private const string CROSSPOINT_ID_ELEMENT = "Id";
		private const string CROSSPOINT_TYPE_ELEMENT = "Type";

		private readonly Dictionary<ushort, eCrosspointType> m_Crosspoints;
		private readonly SafeCriticalSection m_CrosspointsSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public SimplRoomSettings()
		{
			m_Crosspoints = new Dictionary<ushort, eCrosspointType>();
			m_CrosspointsSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Gets the crosspoints.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<ushort, eCrosspointType>> GetCrosspoints()
		{
			return m_CrosspointsSection.Execute(() => m_Crosspoints.OrderByKey().ToArray());
		}

		/// <summary>
		/// Sets the crosspoints.
		/// </summary>
		/// <param name="crosspoints"></param>
		public void SetCrosspoints(IEnumerable<KeyValuePair<ushort, eCrosspointType>> crosspoints)
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

			XmlUtils.WriteDictToXml(writer, m_Crosspoints, CROSSPOINTS_ELEMENT, CROSSPOINT_ELEMENT, CROSSPOINT_ID_ELEMENT,
			                        CROSSPOINT_TYPE_ELEMENT);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			IEnumerable<KeyValuePair<ushort, eCrosspointType>> crosspoints = ReadCrosspointsFromXml(xml);
			SetCrosspoints(crosspoints);
		}

		private static IEnumerable<KeyValuePair<ushort, eCrosspointType>> ReadCrosspointsFromXml(string xml)
		{
			Func<string, ushort> readKey = XmlUtils.ReadElementContentAsUShort;
			Func<string, eCrosspointType> readValue =
				s => XmlUtils.ReadElementContentAsEnum<eCrosspointType>(s, true);

			return XmlUtils.ReadDictFromXml(xml, CROSSPOINTS_ELEMENT, CROSSPOINT_ELEMENT, CROSSPOINT_ID_ELEMENT,
			                                CROSSPOINT_TYPE_ELEMENT, readKey, readValue);
		}

		#endregion
	}
}
