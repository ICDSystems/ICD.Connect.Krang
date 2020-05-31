using ICD.Connect.Telemetry.Attributes;
using ICD.Connect.Telemetry.Providers.External;

namespace ICD.Connect.Krang.Cores
{
	public interface IKrangCoreExternalTelemetryProvider : IExternalTelemetryProvider
	{
		[PropertyTelemetry("ThemeName", null, null)]
		string ThemeName { get; }

		[PropertyTelemetry("ThemeVersion", null, null)]
		string ThemeVersion { get; }

		[PropertyTelemetry("ThemeInformationalVersion", null, null)]
		string ThemeInformationalVersion { get; }
	}
}
