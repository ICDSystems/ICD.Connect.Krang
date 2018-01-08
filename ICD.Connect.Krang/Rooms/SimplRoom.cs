using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Partitioning.Rooms;
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

		public event EventHandler OnVolumeLevelSet;
		public event EventHandler OnVolumeLevelRamp;
		public event EventHandler OnVolumeLevelFeedbackChange;
		public event EventHandler OnVolumeMuteFeedbackChange;

		private readonly Dictionary<ushort, eCrosspointType> m_Crosspoints;
		private readonly SafeCriticalSection m_CrosspointsSection;

		private ushort m_VolumeLevelFeedback;
		private bool m_VolumeMuteFeedback;

		#region Properties

		public ushort VolumeLevelFeedback
		{
			get { return m_VolumeLevelFeedback; }
			private set
			{
				if (value == m_VolumeLevelFeedback)
					return;

				m_VolumeLevelFeedback = value;

				OnVolumeLevelFeedbackChange.Raise(this);
			}
		}

		public bool VolumeMuteFeedback
		{
			get { return m_VolumeMuteFeedback; }
			private set
			{
				if (value == m_VolumeMuteFeedback)
					return;

				m_VolumeMuteFeedback = value;

				OnVolumeMuteFeedbackChange.Raise(this);
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public SimplRoom()
		{
			m_Crosspoints = new Dictionary<ushort, eCrosspointType>();
			m_CrosspointsSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnVolumeLevelSet = null;
			OnVolumeLevelRamp = null;
			OnVolumeLevelFeedbackChange = null;
			OnVolumeMuteFeedbackChange = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		public void SetVolumeLevel(ushort volume)
		{
			OnVolumeLevelSet.Raise(this);
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

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Volume Level", m_VolumeLevelFeedback);
			addRow("Volume Mute", m_VolumeMuteFeedback);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("PrintCrosspoints", "Prints the crosspoints added to the room", () => PrintCrosspoints());
			yield return new GenericConsoleCommand<ushort>("SetVolumeLevel", "SetVolumeLevel <LEVEL>", l => SetVolumeLevel(l));
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Returns a table of the crosspoints in the room.
		/// </summary>
		/// <returns></returns>
		private string PrintCrosspoints()
		{
			m_CrosspointsSection.Enter();

			try
			{
				TableBuilder builder = new TableBuilder("Id", "Type");

				foreach (KeyValuePair<ushort, eCrosspointType> kvp in m_Crosspoints)
					builder.AddRow(kvp.Key, kvp.Value);

				return builder.ToString();
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}
		}

		#endregion
	}
}
