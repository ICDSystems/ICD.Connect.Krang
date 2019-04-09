using System;
using ICD.Connect.Krang.SPlus.Routing;

namespace ICD.Connect.Krang.SPlus.OriginatorInfo.Devices
{
	[Serializable]
	public sealed class SourceBaseListInfo : SourceBaseInfo
	{

		public int Index { get; set; }
		
		public bool IsActive { get; set; }

		public SourceBaseListInfo(IKrangAtHomeSourceBase soureBase, int index, bool active) : base(soureBase)
		{
			if (soureBase == null)
				return;

			Index = index;
			IsActive = active;
		}

		public SourceBaseListInfo()
		{
		}

	}
}