using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Originators;

namespace ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup
{
	public sealed class KrangAtHomeSourceGroup : AbstractOriginator<KrangAtHomeSourceGroupSettings>, IKrangAtHomeSourceGroup
	{

		private IcdOrderedDictionary<int, List<IKrangAtHomeSource>> m_Sources;

		public KrangAtHomeSource.eSourceVisibility SourceVisibility { get; set; }
		
		public IEnumerable<IKrangAtHomeSource> GetSources()
		{
			throw new NotImplementedException();
		}


		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(KrangAtHomeSourceGroupSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_Sources = settings.Sources;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(KrangAtHomeSourceGroupSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Sources = m_Sources;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_Sources = null;
		}

		#endregion
	}
}