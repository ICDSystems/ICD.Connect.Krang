using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices.SPlusShims;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device;
using ICD.Connect.Krang.SPlus.Rooms;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Shim
{
	public delegate void RoomCrosspointCallback(ushort index, ushort crosspointId, ushort crosspointType);

	public abstract class AbstractSPlusUiShim<TDevice> : AbstractSPlusDeviceShim<TDevice>
		where TDevice : class, IKrangAtHomeUiDeviceShimmable
	{

		#region Fields

		private readonly Dictionary<eCrosspointType, ushort> m_Crosspoints;

		private readonly SafeCriticalSection m_CrosspointsSection;

		#endregion


		#region Properties

		/// <summary>
		/// Updates the room crosspoint info (HVAC, Lighting, etc)
		/// </summary>
		[PublicAPI("S+")]
		public RoomCrosspointCallback UpdateRoomCrosspoint { get; set; }

		#endregion


		#region Constructor

		public AbstractSPlusUiShim()
		{
			m_Crosspoints = new Dictionary<eCrosspointType, ushort>();
			m_CrosspointsSection = new SafeCriticalSection();
		}

		#endregion

		/// <summary>
		/// Called when the originator is attached.
		/// Do any actions needed to syncronize
		/// </summary>
		protected override void InitializeOriginator()
		{
			base.InitializeOriginator();
			Originator.RequestDeviceRefresh();
		}


		[PublicAPI("S+")]
		public void SetRoomId(int id)
		{
			if (Originator == null)
				return;
			Originator.SetRoomId(id);
		}

		[PublicAPI("S+")]
		public void SetAudioSourceId(int id)
		{
			if (Originator == null)
				return;
			Originator.SetAudioSourceId(id);
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

		[PublicAPI("S+")]
		public void SetCrosspointType(ushort index, ushort crosspointType)
		{
			if (!EnumUtils.IsDefined(typeof(eCrosspointType), crosspointType))
				return;

			eCrosspointType type = (eCrosspointType)crosspointType;

			if (type == eCrosspointType.None)
				return;

			m_CrosspointsSection.Execute(() => m_Crosspoints[type] = index);
		}

		[PublicAPI("S+")]
		public void ClearCrosspointTypes()
		{
			m_CrosspointsSection.Execute(() => m_Crosspoints.Clear());
		}


		/// <summary>
		/// Sets the given crosspoints on the shim
		/// </summary>
		/// <param name="crosspoints"></param>
		protected void SetCrosspoints(IEnumerable<KeyValuePair<eCrosspointType, ushort>> crosspoints)
		{
			var callback = UpdateRoomCrosspoint;
			if (callback == null)
				return;

			Dictionary<eCrosspointType, ushort> crosspointsDictionary = crosspoints != null
				                                                            ? crosspoints.ToDictionary()
				                                                            : new Dictionary<eCrosspointType, ushort>();

			List<KeyValuePair<eCrosspointType, ushort>> shimCrosspoints = null;

			m_CrosspointsSection.Execute(() => shimCrosspoints = m_Crosspoints.ToList(m_Crosspoints.Count));

			if (shimCrosspoints == null)
				return;

			// Update all the crosspoints on the shim
			foreach (KeyValuePair<eCrosspointType, ushort> kvp in shimCrosspoints)
			{
				ushort crosspointId;
				// If the type is in the room, set the crosspointID, otherwise set 0
				if (crosspointsDictionary.TryGetValue(kvp.Key, out crosspointId))
					callback(kvp.Value, crosspointId, (ushort)kvp.Key);
				else
				{
					callback(kvp.Value, 0, (ushort)kvp.Key);
				}
			}
		}

		/// <summary>
		/// Clears all the crosspoints on the shim
		/// </summary>
		private void ClearCrosspoints()
		{
			var callback = UpdateRoomCrosspoint;
			if (callback == null)
				return;

			List<KeyValuePair<eCrosspointType, ushort>> shimCrosspoints = null;

			m_CrosspointsSection.Execute(() => shimCrosspoints = m_Crosspoints.ToList(m_Crosspoints.Count));

			foreach (var kvp in shimCrosspoints)
			{
				callback(kvp.Value, 0, kvp.Key.ToUShort());
			}

		}

	}
}