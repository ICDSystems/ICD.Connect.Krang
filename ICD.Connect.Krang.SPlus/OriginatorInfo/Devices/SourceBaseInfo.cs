using System;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.OriginatorInfo.Devices
{
	[Serializable]
	public class SourceBaseInfo : AbstractOriginatorInfo
	{
		public eSourceVisibility SourceVisiblity { get; set; }

		public eKrangAtHomeSourceIcon? SourceIcon { get; set; }

		public SourceBaseInfo(IKrangAtHomeSourceBase source)
			: base(source)
		{
			SourceVisiblity = source.SourceVisibility;
			SourceIcon = source.SourceIcon;
			
		}

		public SourceBaseInfo()
		{
		}
	}
}