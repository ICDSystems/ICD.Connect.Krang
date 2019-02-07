using System;
using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Proxy;
using ICD.Connect.Krang.SPlus.OriginatorInfo.Devices;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	[Serializable]
	public sealed class SourceSelected
	{
		public int Index { get; set; }
		public eSourceTypeRouted SourceTypeRouted { get; set; }
		public SourceInfo SourceInfo { get; set; }
	}

	public sealed class SourceSelectedEventArgs : AbstractGenericApiEventArgs<SourceSelected>
	{

		public int Index { get { return Data.Index; } }

		public eSourceTypeRouted
			SourceTypeRouted { get { return Data.SourceTypeRouted; } }

		public SourceInfo SourceInfo { get { return Data.SourceInfo; } }

		public SourceSelectedEventArgs(SourceSelected source) : base(SPlusTouchpanelDeviceApi.EVENT_SOURCE_SELECTED, source)
		{
			
		}

		public SourceSelectedEventArgs(IKrangAtHomeSource source, int index, eSourceTypeRouted sourceTypeRouted)
			: base(SPlusTouchpanelDeviceApi.EVENT_SOURCE_SELECTED, new SourceSelected
			{
				Index = index,
				SourceTypeRouted = sourceTypeRouted,
				SourceInfo = new SourceInfo(source)
			})
		{
		}

		public SourceSelectedEventArgs(SourceInfo sourceInfo, int index, eSourceTypeRouted sourceTypeRouted)
			: base(SPlusTouchpanelDeviceApi.EVENT_SOURCE_SELECTED, new SourceSelected
			{
				Index = index,
				SourceTypeRouted = sourceTypeRouted,
				SourceInfo = sourceInfo
			})
		{
		}
	}
}