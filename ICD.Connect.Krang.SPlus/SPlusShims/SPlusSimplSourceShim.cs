using ICD.Common.Properties;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Settings.SPlusShims;
#if SIMPLSHARP
using ICDPlatformString = Crestron.SimplSharp.SimplSharpString;
#else
using ICDPlatformString = System.String;
#endif

namespace ICD.Connect.Krang.SPlus.SPlusShims
{
	[PublicAPI("S+")]
	public sealed class SPlusSimplSourceShim : AbstractSPlusOriginatorShim<SimplSource>
	{

		public delegate void SPlusSourceInfoCallback(
			ICDPlatformString sourceName, ushort sourceId, ushort sourceControlId, ushort sourceControlType);

		[PublicAPI("S+")]
		public SPlusSourceInfoCallback SPlusSourceInfo { get; set; }

		/// <summary>
		/// Called when the originator is attached.
		/// Do any actions needed to syncronize
		/// </summary>
		protected override void InitializeOriginator()
		{
			base.InitializeOriginator();
			UpdateSource();
		}

		private void UpdateSource()
		{
			SPlusSourceInfoCallback callback = SPlusSourceInfo;
			if (callback == null)
				return;

			if (Originator == null)
				callback(string.Empty, 0, 0, 0);
			else
				callback(Originator.Name, (ushort)Originator.Id, Originator.CrosspointId, Originator.CrosspointType);
		}
	}
}