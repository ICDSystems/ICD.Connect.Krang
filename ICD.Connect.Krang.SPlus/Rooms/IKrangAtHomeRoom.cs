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

		event EventHandler<GenericEventArgs<IVolumeDeviceControl>> OnVolumeControlChanged;

		ISimplSource GetSource();

		void SetSource(ISimplSource source, eSourceTypeRouted routed);

		ISimplSource GetSourceId(int id);

		IVolumeDeviceControl VolumeControl { get; }
	}
}