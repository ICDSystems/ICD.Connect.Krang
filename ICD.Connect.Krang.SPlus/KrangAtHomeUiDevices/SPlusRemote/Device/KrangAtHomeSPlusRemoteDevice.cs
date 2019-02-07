using System;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.EventArgs;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Device
{
	public sealed class KrangAtHomeSPlusRemoteDevice : AbstractKrangAtHomeUiDevice<KrangAtHomeSPlusRemoteDeviceSettings>, IKrangAtHomeSPlusRemoteDeviceShimmable
	{
		public event EventHandler<RoomChangedApiEventArgs> OnRoomChanged;

		public event EventHandler<SourceChangedApiEventArgs> OnSourceChanged;

		internal void RaiseRoomChanged(IKrangAtHomeRoom room)
		{
			OnRoomChanged.Raise(this, new RoomChangedApiEventArgs(room));
		}

		internal void RaiseSourceChanged(IKrangAtHomeSource source)
		{
			OnSourceChanged.Raise(this, new SourceChangedApiEventArgs(source));
		}
	}
}