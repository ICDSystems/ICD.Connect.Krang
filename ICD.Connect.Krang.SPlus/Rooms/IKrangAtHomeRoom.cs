using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Krang.SPlus.Routing;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Krang.SPlus.Themes;
using ICD.Connect.Partitioning.Rooms;

namespace ICD.Connect.Krang.SPlus.Rooms
{
	public interface IKrangAtHomeRoom : IRoom
	{
		event EventHandler OnActiveSourcesChange;

		event EventHandler<GenericEventArgs<IVolumeDeviceControl>> OnActiveVolumeControlChanged;

		string ShortName { get; }

		IKrangAtHomeSource GetSource();

		IEnumerable<IKrangAtHomeSourceBase> GetSourcesBase();

		void SetSource(IKrangAtHomeSourceBase source, eSourceTypeRouted routed);

		IKrangAtHomeSourceBase GetSourceId(int id);

		IVolumeDeviceControl ActiveVolumeControl { get; }


		IEnumerable<KeyValuePair<eCrosspointType, ushort>> GetCrosspoints();

		int GetCrosspointsCount();

		void ApplyTheme(KrangAtHomeTheme krangAtHomeTheme);
	}
}