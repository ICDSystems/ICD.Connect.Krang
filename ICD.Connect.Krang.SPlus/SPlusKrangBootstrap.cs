using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Krang.Cores;
using ICD.Connect.Settings.SPlusShims.GlobalEvents;

namespace ICD.Connect.Krang.SPlus
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
				s_Bootstrap = new KrangBootstrap();
				s_Bootstrap.Krang.OnSettingsApplied += KrangOnSettingsApplied;
				s_Bootstrap.Krang.OnSettingsCleared += KrangOnSettingsCleared;
			}
			catch (Exception e)
			{
				IcdErrorLog.Exception(e.GetBaseException(), "Failed to create KrangBootstrap - {0}", e.GetBaseException().Message);
				throw;
			}
		}

		public static void Start()
		{
			try
			{
				s_Bootstrap.Start();
			}
			catch (Exception e)
			{
				IcdErrorLog.Exception(e.GetBaseException(), "Exception starting Krang - {0}", e.GetBaseException().Message);
			}
		}

		private static void KrangOnSettingsApplied(object sender, EventArgs eventArgs)
		{
			OnKrangLoaded.Raise(null);
			SPlusGlobalEvents.RaiseEvent(new EnvironmentLoadedEventInfo());
		}

		private static void KrangOnSettingsCleared(object sender, EventArgs eventArgs)
		{
			OnKrangCleared.Raise(null);
			SPlusGlobalEvents.RaiseEvent(new EnvironmentUnloadedEventInfo());
		}
	}
}
