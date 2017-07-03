using System;
using Crestron.SimplSharp;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Krang.Core;

#if SIMPLSHARP

namespace ICD.Connect.Krang.SPlusInterfaces
{
	[PublicAPI("SPlus")]
	public static class SPlusKrangBootstrap
	{
		public static event EventHandler OnKrangLoaded;

		public delegate void KrangLoadedCallback();

		private static readonly KrangBootstrap s_Bootstrap;

		public static KrangCore Krang
		{
			get
			{
				return s_Bootstrap == null ? null : s_Bootstrap.Krang;
			}
		}

		static SPlusKrangBootstrap()
		{
			try
			{
				s_Bootstrap = new KrangBootstrap();
				s_Bootstrap.Krang.OnSettingsApplied += KrangOnSettingsApplied;
			}
			catch (Exception e)
			{
				ErrorLog.Exception("Failed to create KrangBootstrap: ", e);
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
				ErrorLog.Exception("Exception starting Krang: ", e);
				IcdErrorLog.Exception(e.GetBaseException(), "Exception starting Krang - {0}", e.GetBaseException().Message);
			}
		}

		private static void KrangOnSettingsApplied(object sender, EventArgs eventArgs)
		{
			OnKrangLoaded.Raise(null);
		}
	}
}

#endif
