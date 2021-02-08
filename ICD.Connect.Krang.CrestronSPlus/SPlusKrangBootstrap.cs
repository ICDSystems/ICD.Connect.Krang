using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Krang.Cores;
using ICD.Connect.Settings.CrestronSPlus.SPlusShims.GlobalEvents;
using ICD.Connect.Settings.Originators;

namespace ICD.Connect.Krang.CrestronSPlus
{
	[PublicAPI("S+")]
	public static class SPlusKrangBootstrap
	{
		public static event EventHandler OnKrangLoaded;
		public static event EventHandler OnKrangCleared;

		private static readonly KrangBootstrap s_Bootstrap;

		public static KrangCore Krang { get { return s_Bootstrap == null ? null : s_Bootstrap.Krang; } }

		static SPlusKrangBootstrap()
		{
			try
			{
				s_Bootstrap = new KrangBootstrap(false);
				s_Bootstrap.Krang.OnLifecycleStateChanged += KrangOnLifecycleStateChanged;
			}
			catch (Exception e)
			{
				IcdErrorLog.Exception(e.GetBaseException(), "Failed to create KrangBootstrap - {0}", e.GetBaseException().Message);
				throw;
			}
		}

		private static void KrangOnLifecycleStateChanged(object sender, LifecycleStateEventArgs args)
		{
			switch (args.Data)
			{
				case eLifecycleState.Started:
					OnKrangLoaded.Raise(null);
					SPlusGlobalEvents.RaiseEvent(new EnvironmentLoadedEventInfo());
					break;
				case eLifecycleState.Cleared:
					OnKrangCleared.Raise(null);
					SPlusGlobalEvents.RaiseEvent(new EnvironmentUnloadedEventInfo());
					break;
			}
		}

		[PublicAPI("S+")]
		public static void Start()
		{
			try
			{
				s_Bootstrap.Start(null);
			}
			catch (Exception e)
			{
				IcdErrorLog.Exception(e.GetBaseException(), "Exception starting Krang - {0}", e.GetBaseException().Message);
			}
		}
	}
}
