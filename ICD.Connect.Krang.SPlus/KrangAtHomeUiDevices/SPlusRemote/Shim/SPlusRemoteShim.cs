using ICD.Common.Properties;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Shim;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Device;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.EventArgs;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
#if SIMPLSHARP
using ICDPlatformString = Crestron.SimplSharp.SimplSharpString;
#else
using ICDPlatformString = System.String;
#endif

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusRemote.Shim
{
	public delegate void RoomInfoCallback(int id, ICDPlatformString name);

	public delegate void SourceInfoCallback(
		int id, ICDPlatformString name, ushort crosspointId, ushort crosspointType);

	[PublicAPI("S+")]
	public sealed class SPlusRemoteShim : AbstractSPlusUiShim<IKrangAtHomeSPlusRemoteDeviceShimmable>
	{

		#region Delegates to S+

		[PublicAPI("S+")]
		public RoomInfoCallback UpdateRoomInfo { get; set; }

		[PublicAPI("S+")]
		public SourceInfoCallback UpdateSourceInfo { get; set; }

		#endregion

		/// <summary>
		/// Subscribes to the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Subscribe(IKrangAtHomeSPlusRemoteDeviceShimmable originator)
		{
			base.Subscribe(originator);

			if (originator == null)
				return;

			originator.OnRoomChanged += OriginatorOnRoomChanged;
			originator.OnSourceChanged += OriginatorOnSourceChanged;
		}

		/// <summary>
		/// Unsubscribes from the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Unsubscribe(IKrangAtHomeSPlusRemoteDeviceShimmable originator)
		{
			base.Unsubscribe(originator);

			if (originator == null)
				return;

			originator.OnRoomChanged -= OriginatorOnRoomChanged;
			originator.OnSourceChanged -= OriginatorOnSourceChanged;
		}

		private void OriginatorOnRoomChanged(object sender, RoomChangedApiEventArgs args)
		{
			SetSPlusRoomInfo(args.Data);
		}

		private void OriginatorOnSourceChanged(object sender, SourceChangedApiEventArgs args)
		{
			SetSPlusSourceInfo(args.Data);
		}

		private void SetSPlusRoomInfo(RoomInfo roomInfo)
		{
			var callback = UpdateRoomInfo;
			if (callback == null)
				return;

			if (roomInfo == null)
				callback(0, "");
			else
				callback(roomInfo.Id, roomInfo.Name);
		}

		private void SetSPlusSourceInfo(SourceInfo sourceInfo)
		{
			var callback = UpdateSourceInfo;
			if (callback == null)
				return;

			if (sourceInfo == null)
				callback(0, "", 0, 0);
			else
				callback(sourceInfo.Id, sourceInfo.Name, sourceInfo.CrosspointId, sourceInfo.CrosspointType);
		}
	}
}