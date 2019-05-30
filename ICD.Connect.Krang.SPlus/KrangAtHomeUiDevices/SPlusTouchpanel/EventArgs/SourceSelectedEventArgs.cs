using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	[Serializable]
	public sealed class SourceSelected
	{
		public int AudioIndex { get; set; }
		public int VideoIndex { get; set; }
		public SourceInfo SourceInfo { get; set; }

		public SourceSelected(SourceInfo sourceInfo, int audioIndex, int videoIndex)
		{
			SourceInfo = sourceInfo;
			AudioIndex = audioIndex;
			VideoIndex = videoIndex;
		}

		public SourceSelected(IKrangAtHomeSource source, int audioIndex, int videoIndex)
		{
			SourceInfo = new SourceInfo(source);
			AudioIndex = audioIndex;
			VideoIndex = videoIndex;
		}

		public SourceSelected()
		{
		}
	}

	public sealed class SourceSelectedEventArgs : GenericEventArgs<SourceSelected>
	{

		public int AudioIndex { get { return Data.AudioIndex; } }
		public int VideoIndex { get { return Data.VideoIndex; } }

		public SourceInfo SourceInfo { get { return Data.SourceInfo; } }

		public SourceSelectedEventArgs(SourceSelected source) : base(source)
		{
			
		}

		public SourceSelectedEventArgs(IKrangAtHomeSource source, int audioIndex, int videoIndex)
			: base(new SourceSelected(source, audioIndex,videoIndex))
		{
		}

		public SourceSelectedEventArgs(SourceInfo sourceInfo, int audioIndex, int videoIndex)
			: base(new SourceSelected(sourceInfo, audioIndex, videoIndex))
		{
		}
	}
}