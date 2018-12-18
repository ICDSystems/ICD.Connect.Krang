using System;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;
using ICD.Connect.Settings.Header;

namespace ICD.Connect.Krang.Remote
{
	public sealed class RemoteCoreSettings : AbstractCoreSettings
	{
		public override string FactoryName { get { return null; } }

		public override Type OriginatorType { get { return typeof(RemoteCore); } }

		public override SettingsCollection OriginatorSettings { get { return null; } }

		/// <summary>
		/// Parses and returns only the header portion from the full XML config.
		/// </summary>
		/// <param name="configXml"></param>
		/// <returns></returns>
		public override ConfigurationHeader GetHeader(string configXml)
		{
			throw new NotImplementedException();
		}
	}
}
