using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.States;
using ICD.Connect.Protocol.Crosspoints;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Sigs;
using ICD.Connect.Routing.Connections;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Pages
{
	public abstract class AbstractAudioVideoEquipmentPage : IDisposable
	{
		private readonly Dictionary<int, ControlCrosspointState> m_Sessions;
		private readonly Dictionary<int, RoomGroupState> m_RoomGroupStates; 

		private readonly SafeCriticalSection m_SessionsSection;

		private readonly EquipmentCrosspoint m_Equipment;
		private readonly KrangAtHomeTheme m_Theme;

		public abstract eConnectionType ConnectionType { get; }

		public EquipmentCrosspoint Equipment { get { return m_Equipment; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="theme"></param>
		/// <param name="equipment"></param>
		protected AbstractAudioVideoEquipmentPage(KrangAtHomeTheme theme, EquipmentCrosspoint equipment)
		{
			if (equipment == null)
				throw new ArgumentNullException("equipment");

			m_Sessions = new Dictionary<int, ControlCrosspointState>();
			m_RoomGroupStates = new Dictionary<int, RoomGroupState>();
			m_SessionsSection = new SafeCriticalSection();

			m_Theme = theme;

			m_Equipment = equipment;
			Subscribe(m_Equipment);
		}

		public void Dispose()
		{
			Unsubscribe(m_Equipment);
		}

		#region Joins

		private void Initialize(int id)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Private Methods

		private ControlCrosspointState LazyLoadCrosspointState(int id)
		{
			m_SessionsSection.Enter();

			try
			{
				ControlCrosspointState crosspointState;
				if (!m_Sessions.TryGetValue(id, out crosspointState))
				{
					crosspointState = new ControlCrosspointState(this, id);
					m_Sessions.Add(id, crosspointState);

					SetRoomGroup(crosspointState, 1);

					Initialize(id);
				}

				return crosspointState;
			}
			finally
			{
				m_SessionsSection.Leave();
			}
		}

		private void SetRoomGroup(ControlCrosspointState crosspointState, int roomGroup)
		{
			m_SessionsSection.Enter();

			try
			{
				foreach (RoomGroupState state in m_RoomGroupStates.Values)
					state.RemoveControlId(crosspointState.ControlCrosspointId);

				RoomGroupState roomGroupState;
				if (!m_RoomGroupStates.TryGetValue(roomGroup, out roomGroupState))
				{
					roomGroupState = new RoomGroupState(this, roomGroup);
					m_RoomGroupStates.Add(roomGroup, roomGroupState);
				}

				roomGroupState.AddControlId(crosspointState.ControlCrosspointId);
			}
			finally
			{
				m_SessionsSection.Leave();
			}
		}

		private IEnumerable<KrangAtHomeSource> GetSources()
		{
			switch (ConnectionType)
			{
				case eConnectionType.Audio:
					return m_Theme.GetAudioSources();

				case eConnectionType.Video:
					return m_Theme.GetVideoSources();
				
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion

		#region Equipment Callbacks

		private void Subscribe(EquipmentCrosspoint equipment)
		{
			equipment.OnControlCrosspointCountChanged += EquipmentOnControlCrosspointCountChanged;
			equipment.OnSendOutputData += EquipmentOnSendOutputData;
		}

		private void Unsubscribe(EquipmentCrosspoint equipment)
		{
			equipment.OnControlCrosspointCountChanged -= EquipmentOnControlCrosspointCountChanged;
			equipment.OnSendOutputData -= EquipmentOnSendOutputData;
		}

		private void EquipmentOnControlCrosspointCountChanged(object sender, IntEventArgs eventArgs)
		{
			foreach (int id in m_Equipment.ControlCrosspoints)
				LazyLoadCrosspointState(id);
		}

		private void EquipmentOnSendOutputData(ICrosspoint sender, CrosspointData data)
		{
			foreach (int id in data.GetControlIds())
				foreach (var item in data.GetSigs())
					HandleSigFromControl(id, item);
		}

		private void HandleSigFromControl(int id, SigInfo sig)
		{
			ControlCrosspointState crosspointState = LazyLoadCrosspointState(id);

			
		}

		#endregion
	}
}
