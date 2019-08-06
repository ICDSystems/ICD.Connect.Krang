using ICD.Connect.API.Attributes;
using ICD.Connect.Krang.SPlus.SPlusRoomGroupControl.Proxy;
using ICD.Connect.Settings.Originators.Simpl;

namespace ICD.Connect.Krang.SPlus.SPlusRoomGroupControl
{
	public interface ISPlusRoomGroupControl : ISimplOriginator
	{
		[ApiMethod(SPlusRoomGroupControlApi.METHOD_ALL_OFF, SPlusRoomGroupControlApi.HELP_METHOD_ALL_OFF)]
		void AllOff();
	}
}