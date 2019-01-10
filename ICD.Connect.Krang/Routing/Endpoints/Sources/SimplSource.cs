﻿using System;
using ICD.Common.Properties;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.Routing.Endpoints.Sources
{
	public sealed class SimplSource : AbstractSource<SimplSourceSettings>
	{
		[Flags]
		public enum eSourceVisibility
		{
			None = 0,
			Audio = 1,
			Video = 2,
		}

		#region Properties

		[PublicAPI]
		public ushort CrosspointId { get; set; }

		[PublicAPI]
		public ushort CrosspointType { get; set; }

		[PublicAPI]
		public eSourceVisibility SourceVisibility { get; set; }

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			CrosspointId = 0;
			CrosspointType = 0;
			SourceVisibility = eSourceVisibility.None;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SimplSourceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.CrosspointId = CrosspointId;
			settings.CrosspointType = CrosspointType;
			settings.SourceVisibility = SourceVisibility;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(SimplSourceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			CrosspointId = settings.CrosspointId;
			CrosspointType = settings.CrosspointType;
			SourceVisibility = settings.SourceVisibility;
		}

		#endregion
	}
}
