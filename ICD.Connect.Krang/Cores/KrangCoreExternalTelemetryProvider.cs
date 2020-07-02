using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Telemetry.Attributes;
using ICD.Connect.Telemetry.Providers.External;
using ICD.Connect.Themes;

namespace ICD.Connect.Krang.Cores
{
	public sealed class KrangCoreExternalTelemetryProvider : AbstractExternalTelemetryProvider<KrangCore>
	{
		[EventTelemetry("ProgramStatusChanged")]
		public event EventHandler<GenericEventArgs<IcdEnvironment.eProgramStatusEventType>> OnProgramStatusChanged; 

		private IcdEnvironment.eProgramStatusEventType m_ProgramStatus;

		#region Properties

		[PropertyTelemetry("ThemeName", null, null)]
		public string ThemeName
		{
			get
			{
				ITheme theme = GetTheme();
				return theme == null ? null : theme.Name;
			}
		}

		[PropertyTelemetry("ThemeVersion", null, null)]
		public string ThemeVersion
		{
			get
			{
				ITheme theme = GetTheme();
				return theme == null ? null : theme.GetType().GetAssembly().GetName().Version.ToString();
			}
		}

		[PropertyTelemetry("ThemeInformationalVersion", null, null)]
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

		[PropertyTelemetry("ProgramStatus", null, "ProgramStatusChanged")]
		public IcdEnvironment.eProgramStatusEventType ProgramStatus
		{
			get { return m_ProgramStatus; }
			set
			{
				if (value == m_ProgramStatus)
					return;
				
				m_ProgramStatus = value;

				OnProgramStatusChanged.Raise(this, new GenericEventArgs<IcdEnvironment.eProgramStatusEventType>(m_ProgramStatus));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangCoreExternalTelemetryProvider()
		{
			IcdEnvironment.OnProgramStatusEvent += IcdEnvironmentOnProgramStatusEvent;

			ProgramStatus = IcdEnvironment.eProgramStatusEventType.Resumed;
		}

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

		private void IcdEnvironmentOnProgramStatusEvent(IcdEnvironment.eProgramStatusEventType type)
		{
			ProgramStatus = type;
		}
	}
}
