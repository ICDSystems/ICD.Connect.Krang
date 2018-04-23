using System;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Remote
{
	public sealed class RemoteCoreSettings : AbstractCoreSettings
	{
		public override string FactoryName { get { return null; } }

		public override Type OriginatorType { get { return typeof(RemoteCore); } }

		public override SettingsCollection OriginatorSettings { get { return null; } }
	}
}
