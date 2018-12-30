using System;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.OriginatorInfo.Devices
{
	[Serializable]
	public sealed class SourceBaseInfo : AbstractOriginatorInfo
	{

		public SourceBaseInfo(IKrangAtHomeSourceBase source)
			: base(source)
		{
		}
	}
}