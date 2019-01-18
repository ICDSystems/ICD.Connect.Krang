using ICD.Connect.Audio.VolumePoints;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Destinations;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.SPlus.VolumePoints
{
	public sealed class KrangAtHomeVolumePoint : AbstractVolumePoint<KrangAtHomeVolumePointSettings>
	{
		public eAudioOption AudioOption { get; private set; }

		#region Settings

		protected override void ApplySettingsFinal(KrangAtHomeVolumePointSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			AudioOption = settings.AudioOption;
		}

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			AudioOption = default(eAudioOption);
		}

		protected override void CopySettingsFinal(KrangAtHomeVolumePointSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.AudioOption = AudioOption;
		}

		#endregion
	}
}