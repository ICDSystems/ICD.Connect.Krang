using ICD.Connect.Krang.Cores;
#if SIMPLSHARP
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.Diagnostics;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Misc.CrestronPro;

namespace ICD.Connect.Core
{
	/// <summary>
	/// The program entry point.
	/// </summary>
	[UsedImplicitly]
	public sealed class KrangControlSystem : CrestronControlSystem
	{
		[UsedImplicitly] private Thread m_StartHandle;

		private readonly KrangBootstrap m_Bootstrap;

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangControlSystem()
		{
			m_Bootstrap = new KrangBootstrap();
			IcdEnvironment.OnProgramStatusEvent += IcdEnvironmentOnProgramStatusEvent;

			Thread.MaxNumberOfUserThreads = 200;

			// Delay other program loads until we are done
			SystemMonitor.ProgramInitialization.ProgramInitializationUnderUserControl = true;

			ProgramInfo.RegisterControlSystem(this);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Invoked before any traffic starts flowing back and forth between the devices and the 
		/// user program.
		/// </summary>
		public override void InitializeSystem()
		{
			m_StartHandle = new Thread(Start, null, Thread.eThreadStartOptions.CreateSuspended) {Priority = Thread.eThreadPriority.LowestPriority};
			m_StartHandle.Start();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Instantiates the room.
		/// </summary>
		/// <param name="unused"></param>
		/// <returns></returns>
		private object Start(object unused)
		{
			m_Bootstrap.Start(PostLoadAction);

			return null;
		}

		private void PostLoadAction()
		{
			SystemMonitor.ProgramInitialization.ProgramInitializationComplete = true;
			IcdEnvironment.SetProgramInitializationComplete();
		}

		/// <summary>
		/// Called when the program is paused, resumed or stopping.
		/// </summary>
		/// <param name="type"></param>
		private void IcdEnvironmentOnProgramStatusEvent(IcdEnvironment.eProgramStatusEventType type)
		{
			switch (type)
			{
				case IcdEnvironment.eProgramStatusEventType.Stopping:
					m_Bootstrap.Stop();
					break;
			}
		}

		#endregion
	}
}

#endif
