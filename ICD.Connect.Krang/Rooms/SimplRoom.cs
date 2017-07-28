using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Rooms;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Rooms
{
	public sealed class SimplRoom : AbstractRoom<SimplRoomSettings>
	{
		public enum eCrosspointType
		{
			None,
			Lighting,
			Hvac
		}

		private readonly Dictionary<ushort, eCrosspointType> m_Crosspoints;
		private readonly SafeCriticalSection m_CrosspointsSection;

		private ushort m_VolumeLevelFeedback;

		private bool m_VolumeMuteFeedback;

		public event EventHandler OnVolumeLevelSet;

		public event EventHandler OnVolumeLevelRamp;

		public event EventHandler OnVolumeLevelFeedbackChange;

		public event EventHandler OnVolumeMuteFeedbackChange;

		public ushort VolumeLevelFeedback
		{
			get { return m_VolumeLevelFeedback; }
			private set
			{
				m_VolumeLevelFeedback = value;
				var changeEvent = OnVolumeLevelFeedbackChange;
				if (changeEvent != null)
					changeEvent.Raise(this);
			}
		}

		public bool VolumeMuteFeedback
		{ get { return m_VolumeMuteFeedback;  }
			private set
			{
				m_VolumeMuteFeedback = value;
				var changeEvent = OnVolumeMuteFeedbackChange;
				if (changeEvent != null)
					changeEvent.Raise(this);
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public SimplRoom()
		{
			m_Crosspoints = new Dictionary<ushort, eCrosspointType>();
			m_CrosspointsSection = new SafeCriticalSection();
		}

		#region Methods

		public void SetVolumeLevel(ushort volume)
		{
			var changeEvent = OnVolumeLevelSet;
			if (changeEvent != null)
				changeEvent.Raise(this);
		}

		public void SetVolumeFeedback(ushort volume)
		{
			VolumeLevelFeedback = volume;
		}

		public void SetMuteFeedback(bool mute)
		{
			VolumeMuteFeedback = mute;
		}

		/// <summary>
		/// Gets the crosspoints.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<ushort, eCrosspointType>> GetCrosspoints()
		{
			return m_CrosspointsSection.Execute(() => m_Crosspoints.OrderByKey().ToArray());
		}

		/// <summary>
		/// Sets the crosspoints.
		/// </summary>
		/// <param name="crosspoints"></param>
		public void SetCrosspoints(IEnumerable<KeyValuePair<ushort, eCrosspointType>> crosspoints)
		{
			m_CrosspointsSection.Enter();

			try
			{
				m_Crosspoints.Clear();
				m_Crosspoints.AddRange(crosspoints);
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}
		}

		//todo: Add routing methods here?

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_CrosspointsSection.Execute(() => m_Crosspoints.Clear());
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SimplRoomSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.SetCrosspoints(GetCrosspoints());
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(SimplRoomSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			SetCrosspoints(settings.GetCrosspoints());
		}

		#endregion
	}
}
