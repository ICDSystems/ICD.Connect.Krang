using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Proxy;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.EventArgs
{
	public sealed class SetRoomIdApiEventArgs : AbstractGenericApiEventArgs<int>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SetRoomIdApiEventArgs(int data) : base(SPlusUiDeviceApi.EVENT_SET_ROOM_ID, data)
		{
		}
	}
}