using System;
using ICD.Common.Properties;
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

		public static Core.KrangCore Krang { get { return s_Bootstrap.Krang; } }

		static SPlusKrangBootstrap()
		{
			s_Bootstrap = new KrangBootstrap();
			s_Bootstrap.Krang.OnSettingsApplied += KrangOnSettingsApplied;
		}

		public static void Start()
		{
			s_Bootstrap.Start();
		}

		private static void KrangOnSettingsApplied(object sender, EventArgs eventArgs)
		{
			OnKrangLoaded.Raise(null);
		}
	}
}

#endif
