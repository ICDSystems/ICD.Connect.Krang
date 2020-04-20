using ICD.Connect.Telemetry;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Krang.Cores
{
	public interface IKrangCoreExternalTelemetryProvider : IExternalTelemetryProvider
	{
		[StaticPropertyTelemetry("ThemeName")]
		string ThemeName { get; }

		[StaticPropertyTelemetry("ThemeVersion")]
		string ThemeVersion { get; }

		[StaticPropertyTelemetry("ThemeInformationalVersion")]
		string ThemeInformationalVersion { get; }
	}
}
