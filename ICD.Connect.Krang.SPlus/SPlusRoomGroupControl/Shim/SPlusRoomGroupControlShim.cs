using ICD.Connect.Settings.SPlusShims;

namespace ICD.Connect.Krang.SPlus.SPlusRoomGroupControl.Shim
{
	public sealed class SPlusRoomGroupControlShim : AbstractSPlusOriginatorShim<ISPlusRoomGroupControl>
	{

		public void AllOff()
		{
			if (Originator != null)
				Originator.AllOff();
		}
	}
}