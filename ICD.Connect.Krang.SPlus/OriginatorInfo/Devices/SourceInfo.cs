using System;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.OriginatorInfo.Devices
{
	[Serializable]
	public sealed class SourceInfo : AbstractOriginatorInfo
	{
		public ushort CrosspointId { get; set; }

		public ushort CrosspointType { get; set; }

		public SourceInfo(IKrangAtHomeSource source) : base(source)
		{
			if (source == null)
				return;

			CrosspointId = source.CrosspointId;
			CrosspointType = source.CrosspointType;
		}

		public SourceInfo()
		{
		}
	}
}