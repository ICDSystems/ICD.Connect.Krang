using ICD.Connect.Partitioning.RoomGroups;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.SPlus.SPlusRoomGroup
{
	public sealed class SPlusRoomGroup : AbstractRoomGroup<SPlusRoomGroupSettings>
	{
		public int Index { get; private set; }

		#region Settings

		protected override void ApplySettingsFinal(SPlusRoomGroupSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			Index = settings.Index;
		}

		protected override void CopySettingsFinal(SPlusRoomGroupSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Index = Index;
		}

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Index = 0;
		}

		#endregion
	}
}