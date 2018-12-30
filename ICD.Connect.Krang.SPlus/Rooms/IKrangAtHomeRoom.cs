using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Controls;
using ICD.Connect.Audio.VolumePoints;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Partitioning.Rooms;

namespace ICD.Connect.Krang.SPlus.Rooms
{
	public interface IKrangAtHomeRoom : IRoom
	{

		event EventHandler OnActiveSourcesChange;

		event EventHandler<GenericEventArgs<IVolumeDeviceControl>> OnActiveVolumeControlChanged;

		IKrangAtHomeSource GetSource();

		void SetSource(IKrangAtHomeSource source, eSourceTypeRouted routed);

		IKrangAtHomeSource GetSourceId(int id);

		IVolumeDeviceControl ActiveVolumeControl { get; }
	}
}