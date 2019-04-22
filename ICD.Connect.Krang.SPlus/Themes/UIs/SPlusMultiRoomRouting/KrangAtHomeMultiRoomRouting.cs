using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Krang.SPlus.RoomGroups;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting.Settings;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting
{
	public sealed class KrangAtHomeMultiRoomRouting
	{
		private readonly List<IKrangAtHomeSource> m_Sources;
		private readonly Dictionary<int, SPlusRoomGroup> m_RoomGroups;

		private EquipmentCrosspointManager m_EquipmentCrosspointManager;

		public int EquipmentId { get; set; }

		public NonCachingEquipmentCrosspoint EquipmentCrosspoint { get; set; }

		public String Name { get; set; }

		public eConnectionType ConnectionType { get; set; }

		public Dictionary<int, SPlusRoomGroup> RoomGroups { get { return m_RoomGroups; } }

		public List<IKrangAtHomeSource> Sources { get { return m_Sources; }}

		public KrangAtHomeMultiRoomRouting()
		{
			m_Sources = new List<IKrangAtHomeSource>();
			m_RoomGroups = new Dictionary<int, SPlusRoomGroup>();
		}

		public void ApplySettings(KrangAtHomeTheme theme, KrangAtHomeMultiRoomRoutingSettings settings, IDeviceFactory factory)
		{
			if (theme == null)
				throw new ArgumentNullException("theme");
			if (settings == null)
				throw new ArgumentNullException("settings");
			if (factory == null)
				throw new ArgumentNullException("factory");

			try
			{
				EquipmentId = settings.EquipmentId;
				Name = settings.Name;
				ConnectionType = settings.ConnectionType;

				Sources.Clear();
				Sources.AddRange(settings.SourceIds.Select(id => factory.GetOriginatorById<IKrangAtHomeSource>(id)));
				RoomGroups.Clear();
				RoomGroups.AddRange(
				                    settings.RoomGroupIds.Select(
				                                                 kvp =>
				                                                 new KeyValuePair<int, SPlusRoomGroup>(kvp.Key,
				                                                                                       factory
					                                                                                       .GetOriginatorById
					                                                                                       <SPlusRoomGroup>
					                                                                                       (kvp.Value))));
				
				EquipmentCrosspoint = new NonCachingEquipmentCrosspoint(EquipmentId, Name);

				m_EquipmentCrosspointManager = theme.EquipmentCrosspointManager;
				m_EquipmentCrosspointManager.RegisterCrosspoint(EquipmentCrosspoint);
				

			}
			catch (Exception e)
			{
				theme.Log(eSeverity.Error, e, "Exception Applying MultiRoomRouting Settings");
			}
		}

		public KrangAtHomeMultiRoomRoutingSettings CopySettings()
		{
			KrangAtHomeMultiRoomRoutingSettings settings = new KrangAtHomeMultiRoomRoutingSettings();

			settings.EquipmentId = EquipmentId;
			settings.Name = Name;
			settings.ConnectionType = ConnectionType;

			settings.SourceIds.AddRange(Sources.Select(s => s.Id));

			RoomGroups.ForEach(kvp => settings.RoomGroupIds.Add(kvp.Key, kvp.Value.Id));

			return settings;
		}

		public void ClearSettings()
		{
			if (EquipmentCrosspoint != null && m_EquipmentCrosspointManager != null)
			{
				m_EquipmentCrosspointManager.UnregisterCrosspoint(EquipmentCrosspoint);
				EquipmentCrosspoint.Dispose();
				EquipmentCrosspoint = null;
				m_EquipmentCrosspointManager = null;
			}

			RoomGroups.Clear();
			Sources.Clear();
			Name = null;
			EquipmentId = 0;
		}
	}
}