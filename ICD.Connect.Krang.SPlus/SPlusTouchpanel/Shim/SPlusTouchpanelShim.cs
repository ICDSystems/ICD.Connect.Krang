using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices.SPlusShims;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.Device;
using ICD.Connect.Krang.SPlus.SPlusTouchpanel.EventArgs;
#if SIMPLSHARP
using ICDPlatformString = Crestron.SimplSharp.SimplSharpString;
#else
using ICDPlatformString = System.String;
#endif

namespace ICD.Connect.Krang.SPlus.SPlusTouchpanel.Shim
{

	public delegate void ListSizeCallback(ushort size);

	public delegate void RoomInfoCallback(int id, ICDPlatformString name, ushort index);

	public delegate void SourceInfoCallback(
		int id, ICDPlatformString name, ushort crosspointId, ushort crosspointType, ushort sourceListIndex,
		ushort sourceIndex);

	public delegate void RoomListCallback(ushort index, int roomId, ICDPlatformString roomName);

	public delegate void SourceListCallback(
		ushort index, int sourceId, ICDPlatformString sourceName);

	public delegate void VolumeFeedbackCallback(ushort data);

	public delegate void VolumeAvailableControlCallback(ushort levelControl, ushort muteControl);

	[PublicAPI("S+")]
	public sealed class SPlusTouchpanelShim : AbstractSPlusDeviceShim<IKrangAtHomeSPlusTouchpanelDevice>
	{

		private const int INDEX_OFFSET_SPLUS = 1;

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

		[PublicAPI("S+")]
		public VolumeAvailableControlCallback UpdateVolumeAvailableControl { get; set; }

		#endregion

		#region SPlus 

		[PublicAPI("S+")]
		public void SetRoomIndex(ushort index)
		{
			if (Originator == null)
				return;
			Originator.SetRoomIndex((ushort)(index - INDEX_OFFSET_SPLUS));
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
			Originator.SetAudioSourceIndex((ushort)(index - INDEX_OFFSET_SPLUS));
		}

		[PublicAPI("S+")]
		public void SetAudioSourceId(int id)
		{
			if (Originator == null)
				return;
			Originator.SetAudioSourceId(id);
		}

		[PublicAPI("S+")]
		public void SetVideoSourceIndex(ushort index)
		{
			if (Originator == null)
				return;
			Originator.SetVideoSourceIndex((ushort)(index - INDEX_OFFSET_SPLUS));
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
		/// <param name="roomInfo"></param>
		/// <param name="index"></param>
		private void SetSPlusRoomInfo(RoomInfo roomInfo, int index)
		{
			var callback = UpdateRoomInfo;
			if (callback != null && roomInfo != null)
				callback(roomInfo.Id, SPlusSafeString(roomInfo.Name), (ushort)(index + INDEX_OFFSET_SPLUS));
		}

		/// <summary>
		/// Updates the room list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="roomList"></param>
		private void SetSPlusRoomList(IList<RoomInfo> roomList)
		{
			RoomListCallback listCallback = UpdateRoomListItem;
			if (listCallback != null)
				for (int i = 0; i < roomList.Count; i++)
				{
					listCallback((ushort)(i + INDEX_OFFSET_SPLUS), roomList[i].Id, SPlusSafeString(roomList[i].Name));
				}

			ListSizeCallback countCallback = UpdateRoomListCount;
			if (countCallback != null)
				countCallback((ushort)roomList.Count);
		}

		/// <summary>
		/// Sets the source info via the delegate
		/// </summary>
		/// <param name="sourceInfo"></param>
		/// <param name="sourceList"></param>
		/// <param name="sourceIndex"></param>
		private void SetSPlusSourceInfo(SourceInfo sourceInfo, ushort sourceList, int sourceIndex)
		{
			SourceInfoCallback callback = UpdateSourceInfo;
			if (callback != null)
				callback(sourceInfo.Id, SPlusSafeString(sourceInfo.Name), sourceInfo.CrosspointId, sourceInfo.CrosspointType, sourceList, (ushort)(sourceIndex + INDEX_OFFSET_SPLUS));
		}

		/// <summary>
		/// Updates the source list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="sourceList"></param>
		private void SetSPlusAudioSourceList(IList<SourceBaseInfo> sourceList)
		{
			SourceListCallback listCallback = UpdateAudioSourceListItem;
			if (listCallback != null)
				for(int i = 0; i < sourceList.Count; i++)
				{
					listCallback((ushort)(i + INDEX_OFFSET_SPLUS), sourceList[i].Id, SPlusSafeString(sourceList[i].Name));
				}

			ListSizeCallback countCallback = UpdateAudioSourceListCount;
			if (countCallback != null)
				countCallback((ushort)sourceList.Count);
		}

		/// <summary>
		/// Updates the source list with the given KVP's.  Key is the index, value is the room;
		/// </summary>
		/// <param name="sourceList"></param>
		private void SetSPlusVideoSourceList(IList<SourceBaseInfo> sourceList)
		{
			SourceListCallback listCallback = UpdateAudioSourceListItem;
			if (listCallback != null)
				for(int i = 0; i < sourceList.Count; i++)
				{
					listCallback((ushort)(i + INDEX_OFFSET_SPLUS), sourceList[i].Id, SPlusSafeString(sourceList[i].Name));
				}

			ListSizeCallback countCallback = UpdateAudioSourceListCount;
			if (countCallback != null)
				countCallback((ushort)sourceList.Count);
		}

		private void SetSPlusVolumeLevelFeedback(float volume)
		{
			VolumeFeedbackCallback callback = UpdateVolumeLevelFeedback;
			if (callback == null)
				return;

			ushort volumeLevel = (ushort)MathUtils.MapRange(0, 1, ushort.MinValue, ushort.MaxValue, volume);

			callback(volumeLevel);
		}

		private void SetSPlusVolumeMuteFeedback(bool mute)
		{
			VolumeFeedbackCallback callback = UpdateVolumeMuteFeedback;
			if (callback == null)
				return;

			callback(mute.ToUShort());
		}

		private void SetSPlusVolumeAvailableControl(eVolumeLevelAvailableControl levelAvailableControl, eVolumeMuteAvailableControl muteAvailableControl)
		{
			VolumeAvailableControlCallback callback = UpdateVolumeAvailableControl;
			if (callback == null)
				return;

			callback((ushort)levelAvailableControl, (ushort)muteAvailableControl);
		}

		#endregion

		#region Originator Callbacks

		/// <summary>
		/// Subscribes to the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Subscribe(IKrangAtHomeSPlusTouchpanelDevice originator)
		{
			base.Subscribe(originator);

			if (originator == null)
				return;

			Originator.OnRoomSelectedUpdate += OriginatorOnRoomSelectedUpdate;
			Originator.OnRoomListUpdate += OriginatorOnRoomListUpdate;
			Originator.OnAudioSourceListUpdate += OriginatorOnAudioSourceListUpdate;
			Originator.OnVideoSourceListUpdate += OriginatorOnVideoSourceListUpdate;
			Originator.OnSourceSelectedUpdate += OriginatorOnSourceSelectedUpdate;
			Originator.OnVolumeLevelFeedbackUpdate += OriginatorOnVolumeLevelFeedbackUpdate;
			Originator.OnVolumeMuteFeedbackUpdate += OriginatorOnVolumeMuteFeedbackUpdate;
			Originator.OnVolumeAvailableControlUpdate += OriginatorOnVolumeAvailableControlUpdate;
		}

		/// <summary>
		/// Unsubscribes from the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Unsubscribe(IKrangAtHomeSPlusTouchpanelDevice originator)
		{
			base.Unsubscribe(originator);

			if (originator == null)
				return;

			Originator.OnRoomSelectedUpdate -= OriginatorOnRoomSelectedUpdate;
			Originator.OnRoomListUpdate -= OriginatorOnRoomListUpdate;
			Originator.OnAudioSourceListUpdate -= OriginatorOnAudioSourceListUpdate;
			Originator.OnVideoSourceListUpdate -= OriginatorOnVideoSourceListUpdate;
			Originator.OnSourceSelectedUpdate -= OriginatorOnSourceSelectedUpdate;
			Originator.OnVolumeLevelFeedbackUpdate -= OriginatorOnVolumeLevelFeedbackUpdate;
			Originator.OnVolumeMuteFeedbackUpdate -= OriginatorOnVolumeMuteFeedbackUpdate;
			Originator.OnVolumeAvailableControlUpdate -= OriginatorOnVolumeAvailableControlUpdate;
		}

		private void OriginatorOnRoomListUpdate(object sender, RoomListEventArgs args)
		{
			SetSPlusRoomList(args.Data);
		}

		private void OriginatorOnRoomSelectedUpdate(object sender, RoomSelectedEventArgs args)
		{
			SetSPlusRoomInfo(args.RoomInfo, (ushort)args.Index);
		}

		private void OriginatorOnVideoSourceListUpdate(object sender, VideoSourceBaseListEventArgs args)
		{
			SetSPlusVideoSourceList(args.Data);
		}

		private void OriginatorOnAudioSourceListUpdate(object sender, AudioSourceBaseListEventArgs args)
		{
			SetSPlusAudioSourceList(args.Data);
		}

		private void OriginatorOnSourceSelectedUpdate(object sender, SourceSelectedEventArgs args)
		{
			SetSPlusSourceInfo(args.SourceInfo, (ushort)args.SourceTypeRouted, (ushort)args.Index);
		}

		private void OriginatorOnVolumeLevelFeedbackUpdate(object sender, VolumeLevelFeedbackEventArgs args)
		{
			SetSPlusVolumeLevelFeedback(args.Data);
		}

		private void OriginatorOnVolumeMuteFeedbackUpdate(object sender, VolumeMuteFeedbackEventArgs args)
		{
			SetSPlusVolumeMuteFeedback(args.Data);
		}

		private void OriginatorOnVolumeAvailableControlUpdate(object sender, VolumeAvailableControlEventArgs args)
		{
			SetSPlusVolumeAvailableControl(args.LevelAvailableControl, args.MuteAvailableControl);
		}

		#endregion
	}
}