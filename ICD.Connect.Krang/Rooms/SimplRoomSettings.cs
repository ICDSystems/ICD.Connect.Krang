using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Rooms
{
	public sealed class SimplRoomSettings : AbstractRoomSettings
	{
		private const string FACTORY_NAME = "SimplRoom";

		private const string CROSSPOINTS_ELEMENT = "Crosspoints";
		private const string CROSSPOINT_ELEMENT = "Crosspoint";
		private const string CROSSPOINT_ID_ELEMENT = "Id";
		private const string CROSSPOINT_TYPE_ELEMENT = "Type";

		private readonly Dictionary<ushort, SimplRoom.eCrosspointType> m_Crosspoints;
		private readonly SafeCriticalSection m_CrosspointsSection;

		#region Properties

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public SimplRoomSettings()
		{
			m_Crosspoints = new Dictionary<ushort, SimplRoom.eCrosspointType>();
			m_CrosspointsSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Gets the crosspoints.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<ushort, SimplRoom.eCrosspointType>> GetCrosspoints()
		{
			return m_CrosspointsSection.Execute(() => m_Crosspoints.OrderByKey().ToArray());
		}

		/// <summary>
		/// Sets the crosspoints.
		/// </summary>
		/// <param name="crosspoints"></param>
		public void SetCrosspoints(IEnumerable<KeyValuePair<ushort, SimplRoom.eCrosspointType>> crosspoints)
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
		/// Creates a new originator instance from the settings.
		/// </summary>
		/// <param name="factory"></param>
		/// <returns></returns>
		public override IOriginator ToOriginator(IDeviceFactory factory)
		{
			SimplRoom output = new SimplRoom();
			output.ApplySettings(this, factory);
			return output;
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlRoomSettingsFactoryMethod(FACTORY_NAME)]
		public static SimplRoomSettings FromXml(string xml)
		{
			IEnumerable<KeyValuePair<ushort, SimplRoom.eCrosspointType>> crosspoints = ReadCrosspointsFromXml(xml);

			SimplRoomSettings output = new SimplRoomSettings();
			output.SetCrosspoints(crosspoints);

			ParseXml(output, xml);
			return output;
		}

		private static IEnumerable<KeyValuePair<ushort, SimplRoom.eCrosspointType>> ReadCrosspointsFromXml(string xml)
		{
			Func<string, ushort> readKey = XmlUtils.ReadElementContentAsUShort;
			Func<string, SimplRoom.eCrosspointType> readValue =
				s => XmlUtils.ReadElementContentAsEnum<SimplRoom.eCrosspointType>(s, true);

			return XmlUtils.ReadDictFromXml(xml, CROSSPOINTS_ELEMENT, CROSSPOINT_ELEMENT, CROSSPOINT_ID_ELEMENT,
			                                CROSSPOINT_TYPE_ELEMENT, readKey, readValue);
		}

		#endregion
	}
}
