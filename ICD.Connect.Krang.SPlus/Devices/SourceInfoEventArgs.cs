using ICD.Common.Utils.EventArguments;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Devices
{
	public class SourceInfoEventArgs : GenericEventArgs<ISimplSource>
	{

		public ushort Index { get; private set; }

		public eSourceTypeRouted SourceTypeRouted { get; private set; }

		public SourceInfoEventArgs(ISimplSource source, ushort index, eSourceTypeRouted sourceTypeRouted)
			: base(source)
		{
			Index = index;
			SourceTypeRouted = sourceTypeRouted;
		}
	}
}