using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.SPlus.Routing.Endpoints.Destinations
{
	public sealed class KrangAtHomeDestination : AbstractDestination<KrangAtHomeDestinationSettings>, IKrangAtHomeDestination
	{
		

		public eAudioOption AudioOption { get; set; }

		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(KrangAtHomeDestinationSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			AudioOption = settings.AudioOption;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			AudioOption = default(eAudioOption);
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(KrangAtHomeDestinationSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.AudioOption = AudioOption;
		}

		#endregion
	}
}