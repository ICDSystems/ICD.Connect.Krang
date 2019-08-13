using System;

namespace ICD.Connect.Krang
{
	public static class KrangSystemKeyAttributes
	{
		public const string MAC_ADDRESS = "MacAddress";

		[Obsolete("Use SYSTEM_KEY_VERSION")]
		public const string LICENSE_VERSION = "LicenseVersion";

		public const string SYSTEM_KEY_VERSION = "SystemKeyVersion";
	}
}
