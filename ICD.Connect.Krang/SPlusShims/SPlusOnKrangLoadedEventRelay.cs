﻿using System;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Krang.SPlusShims
{
	public sealed class SPlusOnKrangLoadedEventRelay : IDisposable
	{
		public event EventHandler OnKrangLoaded;

		public SPlusOnKrangLoadedEventRelay()
		{
			SPlusKrangBootstrap.OnKrangLoaded += SPlusKrangBootstrapOnKrangLoaded;
		}

		public void Dispose()
		{
			SPlusKrangBootstrap.OnKrangLoaded -= SPlusKrangBootstrapOnKrangLoaded;
		}

		private void SPlusKrangBootstrapOnKrangLoaded(object sender, EventArgs eventArgs)
		{
			OnKrangLoaded.Raise(this);
		}
	}
}
