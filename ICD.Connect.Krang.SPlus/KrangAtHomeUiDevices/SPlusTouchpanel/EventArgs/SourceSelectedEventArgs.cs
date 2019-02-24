using System;
using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Proxy;
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
	}

	public sealed class SourceSelectedEventArgs : AbstractGenericApiEventArgs<SourceSelected>
	{

		public int AudioIndex { get { return Data.AudioIndex; } }
		public int VideoIndex { get { return Data.VideoIndex; } }

		public SourceInfo SourceInfo { get { return Data.SourceInfo; } }

		public SourceSelectedEventArgs(SourceSelected source) : base(SPlusTouchpanelDeviceApi.EVENT_SOURCE_SELECTED, source)
		{
			
		}

		public SourceSelectedEventArgs(IKrangAtHomeSource source, int audioIndex, int videoIndex)
			: base(SPlusTouchpanelDeviceApi.EVENT_SOURCE_SELECTED, new SourceSelected
			{
				AudioIndex =  audioIndex,
				VideoIndex =  videoIndex,
				SourceInfo = new SourceInfo(source)
			})
		{
		}

		public SourceSelectedEventArgs(SourceInfo sourceInfo, int audioIndex, int videoIndex)
			: base(SPlusTouchpanelDeviceApi.EVENT_SOURCE_SELECTED, new SourceSelected
			{
				AudioIndex = audioIndex,
				VideoIndex = videoIndex,
				SourceInfo = sourceInfo
			})
		{
		}
	}
}