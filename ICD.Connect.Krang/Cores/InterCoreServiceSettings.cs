﻿using System;
using System.Collections.Generic;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Krang.Remote;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Services;

namespace ICD.Connect.Krang.Cores
{
	[KrangSettings("InterCore", typeof(InterCoreService))]
	public sealed class InterCoreServiceSettings : AbstractServiceSettings
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
		public InterCoreServiceSettings()
		{
			m_Addresses = new IcdHashSet<string>();
		}

		#region Methods

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
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);
	
			Enabled = XmlUtils.TryReadChildElementContentAsBoolean(xml, ELEMENT_ENABLED) ?? false;

			IEnumerable<string> addresses =
				XmlUtils.ReadListFromXml(xml, ELEMENT_ADDRESSES, ELEMENT_ADDRESS, e => XmlUtils.ReadElementContent(e));

			SetAddresses(addresses);
		}

		/// <summary>
		/// Writes the routing settings to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteElementString(ELEMENT_ENABLED, IcdXmlConvert.ToString(Enabled));

			XmlUtils.WriteListToXml(writer, GetAddresses(), ELEMENT_ADDRESSES, ELEMENT_ADDRESS);
		}

		#endregion
	}
}
