using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Krang.SPlus
{
	public sealed class SPlusKrangEventRelay : IDisposable
	{
		/// <summary>
		/// Raised when Krang loads
		/// </summary>
		[PublicAPI("S+")]
		public event EventHandler OnKrangLoaded;

		/// <summary>
		/// Raised when Krang unloads
		/// </summary>
		[PublicAPI("S+")]
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
