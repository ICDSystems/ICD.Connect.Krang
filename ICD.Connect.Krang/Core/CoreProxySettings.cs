using System;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Core
{
	public sealed class CoreProxySettings : AbstractCoreSettings
	{
		public override string FactoryName { get { return null; } }

		public override Type OriginatorType { get { return typeof(CoreProxy); } }

		public override SettingsCollection OriginatorSettings { get { return null; } }
	}
}