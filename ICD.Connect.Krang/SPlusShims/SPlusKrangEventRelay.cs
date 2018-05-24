using System;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Krang.SPlusShims
{
	public sealed class SPlusKrangEventRelay : IDisposable
	{
		public event EventHandler OnKrangLoaded;
		public event EventHandler OnKrangCleared;

		public SPlusKrangEventRelay()
		{
			SPlusKrangBootstrap.OnKrangLoaded += SPlusKrangBootstrapOnKrangLoaded;
			SPlusKrangBootstrap.OnKrangCleared += SPlusKrangBootstrapOnKrangCleared;
		}

		public void Dispose()
		{
			SPlusKrangBootstrap.OnKrangLoaded -= SPlusKrangBootstrapOnKrangLoaded;
			SPlusKrangBootstrap.OnKrangCleared -= SPlusKrangBootstrapOnKrangCleared;
		}

		private void SPlusKrangBootstrapOnKrangLoaded(object sender, EventArgs eventArgs)
		{
			OnKrangLoaded.Raise(this);
		}

		private void SPlusKrangBootstrapOnKrangCleared(object sender, EventArgs eventArgs)
		{
			OnKrangCleared.Raise(this);
		}
	}
}
