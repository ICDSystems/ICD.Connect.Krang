using System;
using ICD.Connect.Settings.Cores;
using Newtonsoft.Json;

namespace ICD.Connect.Krang.Remote.Broadcast.CoreDiscovery
{
	[Serializable]
	public sealed class CoreDiscoveryData
	{
		public int Id { get; set; }
		public string Name { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		[JsonConstructor]
		public CoreDiscoveryData()
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="core"></param>
		public CoreDiscoveryData(ICore core)
			: this()
		{
			Id = core.Id;
			Name = core.Name;
		}
	}
}
