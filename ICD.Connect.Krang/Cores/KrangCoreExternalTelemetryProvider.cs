using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Telemetry.Providers.External;
using ICD.Connect.Themes;

namespace ICD.Connect.Krang.Cores
{
	public sealed class KrangCoreExternalTelemetryProvider : AbstractExternalTelemetryProvider<KrangCore>,
	                                                         IKrangCoreExternalTelemetryProvider
	{
		#region Properties

		public string ThemeName
		{
			get
			{
				ITheme theme = GetTheme();
				return theme == null ? null : theme.Name;
			}
		}

		public string ThemeVersion
		{
			get
			{
				ITheme theme = GetTheme();
				return theme == null ? null : theme.GetType().GetAssembly().GetName().Version.ToString();
			}
		}

		public string ThemeInformationalVersion
		{
			get
			{
				ITheme theme = GetTheme();
				string version;
				return theme == null
						   ? null
						   : theme.GetType().GetAssembly().TryGetInformationalVersion(out version)
								 ? version
								 : null;
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the first theme in the system.
		/// </summary>
		/// <returns></returns>
		[CanBeNull]
		private ITheme GetTheme()
		{
			return Parent.Originators
			             .GetChildren<ITheme>()
			             .FirstOrDefault();
		}

		#endregion
	}
}
