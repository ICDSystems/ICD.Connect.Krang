using System;
using ICD.Common.Utils.Extensions;

#if SIMPLSHARP

namespace ICD.Connect.Krang.SPlusInterfaces
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

#endif
