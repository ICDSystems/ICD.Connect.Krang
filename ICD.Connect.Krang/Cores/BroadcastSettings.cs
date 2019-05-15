using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Krang.Cores
{
	public sealed class BroadcastSettings
	{
		private const string ELEMENT_ENABLED = "Enabled";
		private const string ELEMENT_ADDRESSES = "Addresses";
		private const string ELEMENT_ADDRESS = "Address";

		private readonly IcdHashSet<string> m_Addresses;

		#region Properties

		/// <summary>
		/// Gets/sets broadcasting enabled state.
		/// </summary>
		public bool Enabled { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public BroadcastSettings()
		{
			m_Addresses = new IcdHashSet<string>();
		}

		#region Methods

		/// <summary>
		/// Clears the configured data.
		/// </summary>
		public void Clear()
		{
			Enabled = false;

			SetAddresses(Enumerable.Empty<string>());
		}

		/// <summary>
		/// Copies the settings from the given other settings instance.
		/// </summary>
		/// <param name="settings"></param>
		public void Update(BroadcastSettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException("settings");

			Enabled = settings.Enabled;

			SetAddresses(settings.GetAddresses());
		}

		/// <summary>
		/// Sets the addresses to be broadcast to.
		/// </summary>
		/// <param name="addresses"></param>
		public void SetAddresses(IEnumerable<string> addresses)
		{
			if (addresses == null)
				throw new ArgumentNullException("addresses");

			m_Addresses.Clear();
			m_Addresses.AddRange(addresses);
		}

		/// <summary>
		/// Gets the addresses to be broadcast to.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetAddresses()
		{
			return m_Addresses.ToArray(m_Addresses.Count);
		}

		#endregion

		#region Serialization

		/// <summary>
		/// Updates the settings from the given xml element.
		/// </summary>
		/// <param name="xml"></param>
		public void ParseXml(string xml)
		{
			Enabled = XmlUtils.TryReadChildElementContentAsBoolean(xml, ELEMENT_ENABLED) ?? false;

			IEnumerable<string> addresses =
				XmlUtils.ReadListFromXml(xml, ELEMENT_ADDRESSES, ELEMENT_ADDRESS, e => XmlUtils.ReadElementContent(e));

			SetAddresses(addresses);
		}

		/// <summary>
		/// Writes the current configuration to the given XML writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="element"></param>
		public void ToXml(IcdXmlTextWriter writer, string element)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteStartElement(element);
			{
				writer.WriteElementString(ELEMENT_ENABLED, IcdXmlConvert.ToString(Enabled));

				XmlUtils.WriteListToXml(writer, GetAddresses(), ELEMENT_ADDRESSES, ELEMENT_ADDRESS);
			}
			writer.WriteEndElement();
		}

		#endregion
	}
}
