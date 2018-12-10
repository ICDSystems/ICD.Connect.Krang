using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices.SPlusShims;
using ICD.Connect.Krang.SPlus.Devices;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
#if SIMPLSHARP
using ICDPlatformString = Crestron.SimplSharp.SimplSharpString;
#else
using ICDPlatformString = System.String;
#endif

namespace ICD.Connect.Krang.SPlus.SPlusShims
{

	public delegate void ListSizeCallback(ushort size);

	public delegate void RoomInfoCallback(int id, ICDPlatformString name, ushort index);

	public delegate void SourceInfoCallback(
		int id, ICDPlatformString name, ushort crosspointId, ushort crosspointType, ushort sourceListIndex,
		ushort sourceIndex);

	public delegate void RoomListCallback(ushort index, int roomId, ICDPlatformString roomName);

	public delegate void SourceListCallback(
		ushort index, int sourceId, ICDPlatformString sourceName, ushort crosspointId,
		ushort crosspointType);

	public delegate void VolumeFeedbackCallback(ushort data);

	[PublicAPI("S+")]
	public sealed class SPlusTouchpanelShim : AbstractSPlusDeviceShim<KrangAtHomeSPlusTouchpanelDevice>
	{

		#region S+ Properties

		/// <summary>
		/// Raises the room info when the wrapped room changes.
		/// </summary>
		[PublicAPI("S+")]
		public RoomInfoCallback UpdateRoomInfo { get; set; }

		/// <summary>
		/// Raises for each source that is routed to the room destinations.
		/// </summary>
		[PublicAPI("S+")]
		public SourceInfoCallback UpdateSourceInfo { get; set; }

		/// <summary>
		/// Raised to update the rooms list for new rooms
		/// </summary>
		[PublicAPI("S+")]
		public RoomListCallback UpdateRoomListItem { get; set; }

		[PublicAPI("S+")]
		public ListSizeCallback UpdateRoomListCount { get; set; }

		[PublicAPI("S+")]
		public SourceListCallback UpdateAudioSourceListItem { get; set; }

		[PublicAPI("S+")]
		public ListSizeCallback UpdateAudioSourceListCount { get; set; }

		[PublicAPI("S+")]
		public SourceListCallback UpdateVideoSourceListItem { get; set; }

		[PublicAPI("S+")]
		public ListSizeCallback UpdateVideoSourceListCount { get; set; }

		[PublicAPI("S+")]
		public VolumeFeedbackCallback UpdateVolumeLevelFeedback { get; set; }

		[PublicAPI("S+")]
		public VolumeFeedbackCallback UpdateVolumeMuteFeedback { get; set; }

		#endregion

		#region SPlus 

		[PublicAPI("S+")]
		public void SetRoomIndex(ushort index)
		{
			if (Originator == null)
				return;
			Originator.SetRoomIndex(index);
		}

		[PublicAPI("S+")]
		public void SetRoomId(int id)
		{
			if (Originator == null)
				return;
			Originator.SetRoomId(id);
		}

		[PublicAPI("S+")]
		public void SetAudioSourceIndex(ushort index)
		{
			if (Originator == null)
				return;
			Originator.SetAudioSourceIndex(index);
		}

		[PublicAPI("S+")]
		public void SetAudioSourceId(int id)
		{
			if (Originator == null)
				return;
			Originator.SetAudioSourcdId(id);
		}

		[PublicAPI("S+")]
		public void SetVideoSourceIndex(ushort index)
		{
			if (Originator == null)
				return;
			Originator.SetVideoSourceIndex(index);
		}

		[PublicAPI("S+")]
		public void SetVideoSourceId(int id)
		{
			if (Originator == null)
				return;
			Originator.SetVideoSourcdId(id);
		}

		[PublicAPI("S+")]
		public void SetVolumeLevel(ushort level)
		{
			if (Originator == null)
				return;

			Originator.SetVolumeLevel(MathUtils.MapRange((float)ushort.MinValue, ushort.MaxValue, 0, 1, level));
		}

		[PublicAPI("S+")]
		public void SetVolumeRampUp()
		{
			if (Originator == null)
				return;

			Originator.SetVolumeRampUp();
		}

		[PublicAPI("S+")]
		public void SetVolumeRampDown()
		{
			if (Originator == null)
				return;

			Originator.SetVolumeRampDown();
		}

		[PublicAPI("S+")]
		public void SetVolumeRampStop()
		{
			if (Originator == null)
				return;

			Originator.SetVolumeRampStop();
		}

		[PublicAPI("S+")]
		public void SetVolumeMute(ushort state)
		{
			if (Originator == null)
				return;

			Originator.SetVolumeMute(state.ToBool());
		}

		[PublicAPI("S+")]
		public void SetVolumeMuteToggle()
		{
			if (Originator == null)
				return;

			Originator.SetVolumeMuteToggle();
		}

		#endregion

		#region Private/Protected Methods

		/// <summary>
		/// Called when the originator is attached.
		/// Do any actions needed to syncronize
		/// </summary>
		protected override void InitializeOriginator()
		{
			base.InitializeOriginator();
			Originator.RequestDeviceRefresh();
		}

		/// <summary>
		/// Sets the room info via the delegate
		/// </summary>
		/// <param name="room"></param>
		/// <param name="index"></param>
		private void SetSPlusRoomInfo(IKrangAtHomeRoom room, ushort index)
		{
			var callback = UpdateRoomInfo;
			if (callback != null)
				callback(room.Id, room.Name, index);
		}

		/// <summary>
		/// Updates the room list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="roomList"></param>
		private void SetSPlusRoomList(IEnumerable<KeyValuePair<ushort, IKrangAtHomeRoom>> roomList)
		{
			ushort count = 0;
			RoomListCallback listCallback = UpdateRoomListItem;
			if (listCallback != null)
				foreach (var kvp in roomList)
				{
					listCallback(kvp.Key, kvp.Value.Id, kvp.Value.Name);
					count++;
				}

			ListSizeCallback countCallback = UpdateRoomListCount;
			if (countCallback != null)
				countCallback(count);
		}

		/// <summary>
		/// Sets the source info via the delegate
		/// </summary>
		/// <param name="source"></param>
		/// <param name="sourceList"></param>
		/// <param name="sourceIndex"></param>
		private void SetSPlusSourceInfo(ISimplSource source, ushort sourceList, ushort sourceIndex)
		{
			SourceInfoCallback callback = UpdateSourceInfo;
			if (callback != null)
				callback(source.Id, source.Name, source.CrosspointId, source.CrosspointType, sourceList, sourceIndex);
		}

		/// <summary>
		/// Updates the source list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="sourceList"></param>
		private void SetSPlusAudioSourceList(IEnumerable<KeyValuePair<ushort, ISimplSource>> sourceList)
		{
			ushort count = 0;
			SourceListCallback listCallback = UpdateAudioSourceListItem;
			if (listCallback != null)
				foreach (var kvp in sourceList)
				{
					listCallback(kvp.Key, kvp.Value.Id, kvp.Value.Name, kvp.Value.CrosspointId,
								 kvp.Value.CrosspointType);
					count++;
				}

			ListSizeCallback countCallback = UpdateAudioSourceListCount;
			if (countCallback != null)
				countCallback(count);
		}

		/// <summary>
		/// Updates the source list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="sourceList"></param>
		private void SetSPlusVideoSourceList(IEnumerable<KeyValuePair<ushort, ISimplSource>> sourceList)
		{
			ushort count = 0;
			SourceListCallback listCallback = UpdateAudioSourceListItem;
			if (listCallback != null)
				foreach (var kvp in sourceList)
				{
					listCallback(kvp.Key, kvp.Value.Id, kvp.Value.Name, kvp.Value.CrosspointId,
								 kvp.Value.CrosspointType);
					count++;
				}

			ListSizeCallback countCallback = UpdateAudioSourceListCount;
			if (countCallback != null)
				countCallback(count);
		}

		private void SetSPlusVolumeLevelFeedback(float volume)
		{
			var callback = UpdateVolumeLevelFeedback;
			if (callback == null)
				return;

			ushort volumeLevel = (ushort)MathUtils.MapRange(0, 1, ushort.MinValue, ushort.MaxValue, volume);

			callback(volumeLevel);
		}

		private void SetSPlusVolumeMuteFeedback(bool mute)
		{
			var callback = UpdateVolumeMuteFeedback;
			if (callback == null)
				return;

			callback(mute.ToUShort());
		}


		#endregion

		#region Originator Callbacks

		/// <summary>
		/// Subscribes to the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Subscribe(KrangAtHomeSPlusTouchpanelDevice originator)
		{
			base.Subscribe(originator);

			if (originator == null)
				return;

			Originator.OnRoomInfoUpdate += OriginatorOnRoomInfoUpdate;
			Originator.OnRoomListUpdate += OriginatorOnRoomListUpdate;
			Originator.OnAudioSourceListUpdate += OriginatorOnAudioSourceListUpdate;
			Originator.OnVideoSourceListUpdate += OriginatorOnVideoSourceListUpdate;
			Originator.OnSourceInfoUpdate += OriginatorOnSourceInfoUpdate;
			Originator.OnVolumeLevelFeedbackUpdate += OriginatorOnVolumeLevelFeedbackUpdate;
			Originator.OnVolumeMuteFeedbackUpdate += OriginatorOnVolumeMuteFeedbackUpdate;
		}

		/// <summary>
		/// Unsubscribes from the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Unsubscribe(KrangAtHomeSPlusTouchpanelDevice originator)
		{
			base.Unsubscribe(originator);

			if (originator == null)
				return;

			Originator.OnRoomInfoUpdate -= OriginatorOnRoomInfoUpdate;
			Originator.OnRoomListUpdate -= OriginatorOnRoomListUpdate;
			Originator.OnAudioSourceListUpdate -= OriginatorOnAudioSourceListUpdate;
			Originator.OnVideoSourceListUpdate -= OriginatorOnVideoSourceListUpdate;
			Originator.OnSourceInfoUpdate -= OriginatorOnSourceInfoUpdate;
			Originator.OnVolumeLevelFeedbackUpdate -= OriginatorOnVolumeLevelFeedbackUpdate;
		}

		private void OriginatorOnRoomListUpdate(object sender, GenericEventArgs<IEnumerable<KeyValuePair<ushort, IKrangAtHomeRoom>>> args)
		{
			SetSPlusRoomList(args.Data);
		}

		private void OriginatorOnRoomInfoUpdate(object sender, RoomInfoEventArgs args)
		{
			SetSPlusRoomInfo(args.Data, args.Index);
		}

		private void OriginatorOnVideoSourceListUpdate(object sender, GenericEventArgs<IEnumerable<KeyValuePair<ushort, ISimplSource>>> args)
		{
			SetSPlusVideoSourceList(args.Data);
		}

		private void OriginatorOnAudioSourceListUpdate(object sender, GenericEventArgs<IEnumerable<KeyValuePair<ushort, ISimplSource>>> args)
		{
			SetSPlusAudioSourceList(args.Data);
		}

		private void OriginatorOnSourceInfoUpdate(object sender, SourceInfoEventArgs args)
		{
			SetSPlusSourceInfo(args.Data, (ushort)args.SourceTypeRouted, args.Index);
		}

		private void OriginatorOnVolumeLevelFeedbackUpdate(object sender, FloatEventArgs args)
		{
			SetSPlusVolumeLevelFeedback(args.Data);
		}

		private void OriginatorOnVolumeMuteFeedbackUpdate(object sender, BoolEventArgs args)
		{
			SetSPlusVolumeMuteFeedback(args.Data);
		}

		#endregion
	}
}