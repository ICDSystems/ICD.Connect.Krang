﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Permissions;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Attributes;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Krang.Remote;
using ICD.Connect.Partitioning;
using ICD.Connect.Partitioning.PartitionManagers;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Groups.Endpoints.Destinations;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Cores;
using ICD.Connect.Settings.Originators;
using ICD.Connect.Settings.Services;
using ICD.Connect.Settings.Utils;
using ICD.Connect.Telemetry.Attributes;
using ICD.Connect.Telemetry.Services;
using ICD.Connect.Themes;

namespace ICD.Connect.Krang.Cores
{
	[ExternalTelemetry("Krang Core Telemetry", typeof(KrangCoreExternalTelemetryProvider))]
	public sealed class KrangCore : AbstractCore<KrangCoreSettings>
	{
		/// <summary>
		/// Originator ids are pushed to the stack on load, and popped on clear.
		/// </summary>
		private readonly Stack<int> m_LoadedOriginators;

		#region Properties

		/// <summary>
		/// Gets the name of the node in the console.
		/// </summary>
		public override string ConsoleName { get { return "Core"; } }

		/// <summary>
		/// Gets the routing graph for the program.
		/// </summary>
		[CanBeNull]
		[ApiNode("Routing", "The routing features for the core.")]
		public RoutingGraph RoutingGraph
		{
			get { return Originators.GetChildren<RoutingGraph>().SingleOrDefault(); }
		}

		/// <summary>
		/// Gets the partition manager for the program.
		/// </summary>
		[CanBeNull]
		[ApiNode("Partitioning", "The partitioning features for the core.")]
		public PartitionManager PartitionManager
		{
			get { return Originators.GetChildren<PartitionManager>().SingleOrDefault(); }
		}

		/// <summary>
		/// Gets the core telemetry instance.
		/// </summary>
		[CanBeNull]
		[ApiNode("TelemetryService", "The telemetry available for this system.")]
		public TelemetryService TelemetryService
		{
			get { return Originators.GetChildren<TelemetryService>().SingleOrDefault(); }
		}

		/// <summary>
		/// Gets the inter-core service.
		/// </summary>
		[CanBeNull]
		[ApiNode("InterCoreService", "The inter-core communication features.")]
		public InterCoreService InterCoreService
		{
			get { return Originators.GetChildren<InterCoreService>().SingleOrDefault(); }
		}

		[ApiNodeGroup("Themes", "The currently active themes")]
		private IApiNodeGroup Themes { get; set; }

		[ApiNodeGroup("Devices", "The currently active devices")]
		private IApiNodeGroup Devices { get; set; }

		[ApiNodeGroup("Ports", "The currently active ports")]
		private IApiNodeGroup Ports { get; set; }

		[ApiNodeGroup("Rooms", "The currently active rooms")]
		private IApiNodeGroup Rooms { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangCore()
		{
			ServiceProvider.AddService<ICore>(this);

			m_LoadedOriginators = new Stack<int>();

			Themes = new ApiOriginatorsNodeGroup<ITheme>(Originators);
			Devices = new ApiOriginatorsNodeGroup<IDevice>(Originators);
			Ports = new ApiOriginatorsNodeGroup<IPort>(Originators);
			Rooms = new ApiOriginatorsNodeGroup<IRoom>(Originators);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			DisposeOriginators();
		}

		/// <summary>
		/// Adds the given originator to the core.
		/// </summary>
		/// <param name="originator"></param>
		private void AddOriginator(IOriginator originator)
		{
			if (originator == null)
				throw new ArgumentNullException("originator");

			Originators.AddChild(originator);
		}

		/// <summary>
		/// Unsubscribes, disposes and clears the devices.
		/// </summary>
		private void DisposeOriginators()
		{
			Dictionary<int, IOriginator> originators = Originators.ToDictionary(o => o.Id);
			IcdHashSet<IOriginator> disposed = new IcdHashSet<IOriginator>();

			// First dispose the telemetry service so we don't push telemetry during teardown
			if (TelemetryService != null)
			{
				TelemetryService.Providers
				                .ForEach(p =>
				                         {
					                         TryDisposeOriginator(p);
					                         disposed.Add(p);
				                         });

				TryDisposeOriginator(TelemetryService);
				disposed.Add(TelemetryService);
			}

			// Then empty all of the rooms
			foreach (IRoom room in originators.Values.OfType<IRoom>())
				room.Originators.Clear();

			// Try to dispose in reverse of load order
			while (m_LoadedOriginators.Count > 0)
			{
				int id = m_LoadedOriginators.Pop();

				IOriginator originator;
				if (!originators.TryGetValue(id, out originator) || disposed.Contains(originator))
					continue;

				TryDisposeOriginator(originator);
				disposed.Add(originator);
			}

			// Now dispose the remainder
			foreach (IOriginator originator in originators.Values.Where(o => !disposed.Contains(o)))
				TryDisposeOriginator(originator);

			Originators.Clear();
		}

		/// <summary>
		/// Attempts to dispose the originator if it implements IDisposable. Logs any exceptions.
		/// </summary>
		/// <param name="originator"></param>
		private void TryDisposeOriginator(IOriginator originator)
		{
			if (originator == null)
				throw new ArgumentNullException("originator");

			try
			{
				IDisposable disposable = originator as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, e, "Failed to dispose {0} - {1}", originator, e.Message);
			}
		}

		private void SetupDestinationGroupsInRooms(IEnumerable<IDestinationGroup> destinationGroups, IEnumerable<IRoom> rooms)
		{
			var roomsArray = rooms.ToArray();
			foreach (IDestinationGroup destinationGroup in destinationGroups)
			{
				foreach (IRoom room in roomsArray)
				{
					IRoom roomLocalVariable = room;
					if (destinationGroup
						.GetItems()
						.AnyAndAll(destination => roomLocalVariable.Originators.ContainsRecursive(destination.Id)))
						room.Originators.Add(destinationGroup.Id, eCombineMode.Always);
				}
			}
		}

		private IEnumerable<IDestinationGroup> SetupDestinationGroupsFromDestinationGroupString(IRoutingGraph routingGraph)
		{
			IcdHashSet<IDestinationGroup> destinationGroups = new IcdHashSet<IDestinationGroup>();

			foreach (IDestination destination in routingGraph.Destinations.Where(d => !string.IsNullOrEmpty(d.Group)))
			{
				IDestinationGroup destinationGroup = LazyLoadDestinationGroupByName(routingGraph, destination.Group);
				destinationGroups.Add(destinationGroup);
				destinationGroup.AddItem(destination);
			}

			return destinationGroups;
		}

		/// <summary>
		/// Gets the destination group with the given name
		/// </summary>
		/// <param name="routingGraph"></param>
		/// <param name="name"></param>
		/// <returns>DestinationGroup</returns>
		private IDestinationGroup LazyLoadDestinationGroupByName(IRoutingGraph routingGraph, string name)
		{
			IDestinationGroup destinationGroup = routingGraph.DestinationGroups.GetChildren().FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.InvariantCultureIgnoreCase));

			if (destinationGroup != null)
				return destinationGroup;

			int id = GetDestinationGroupId();

			destinationGroup = new DestinationGroup
			{
				Name = name,
				Id = id,
				Uuid = OriginatorUtils.GenerateUuid(Core, id),
				ConnectionTypeMask = eConnectionType.Video | eConnectionType.Audio | eConnectionType.Usb,
				Serialize = true
			};

			routingGraph.DestinationGroups.AddChild(destinationGroup);
			Originators.AddChild(destinationGroup);

			return destinationGroup;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(KrangCoreSettings settings)
		{
			base.CopySettingsFinal(settings);

			// Clear the old originators
			settings.OriginatorSettings.Clear();

			// Inter-Core
			InterCoreService interCore = InterCoreService;
			InterCoreServiceSettings interCoreSettings =
				interCore == null || !interCore.Serialize
					? new InterCoreServiceSettings
					{
						Id = IdUtils.GetNewId(Originators.GetChildrenIds(), IdUtils.ID_INTER_CORE)
					}
					: interCore.CopySettings();
			settings.OriginatorSettings.Add(interCoreSettings);

			settings.OriginatorSettings.AddRange(interCoreSettings.ProviderSettings);

			// Telemetry
			TelemetryService telemetry = TelemetryService;
			TelemetryServiceSettings telemetrySettings =
				telemetry == null || !telemetry.Serialize
					? new TelemetryServiceSettings
					{
						Id = IdUtils.GetNewId(Originators.GetChildrenIds(), IdUtils.ID_TELEMETRY)
					}
					: telemetry.CopySettings();
			settings.OriginatorSettings.Add(telemetrySettings);

			settings.OriginatorSettings.AddRange(telemetrySettings.ProviderSettings);
			
			// Routing
			RoutingGraph routingGraph = RoutingGraph;
			RoutingGraphSettings routingSettings =
				routingGraph == null || !routingGraph.Serialize
					? new RoutingGraphSettings
					{
						Id = IdUtils.GetNewId(Originators.GetChildrenIds(), IdUtils.ID_ROUTING_GRAPH)
					}
					: routingGraph.CopySettings();
			settings.OriginatorSettings.Add(routingSettings);

			settings.OriginatorSettings.AddRange(routingSettings.ConnectionSettings);
			settings.OriginatorSettings.AddRange(routingSettings.StaticRouteSettings);
			settings.OriginatorSettings.AddRange(routingSettings.SourceSettings);
			settings.OriginatorSettings.AddRange(routingSettings.DestinationSettings);
			settings.OriginatorSettings.AddRange(routingSettings.SourceGroupSettings);
			settings.OriginatorSettings.AddRange(routingSettings.DestinationGroupSettings);

			// Partitioning
			PartitionManager partitionManager = PartitionManager;
			PartitionManagerSettings partitionSettings =
				partitionManager == null || !partitionManager.Serialize
					? new PartitionManagerSettings
					{
						Id = IdUtils.GetNewId(Originators.GetChildrenIds(), IdUtils.ID_PARTITION_MANAGER)
					}
					: partitionManager.CopySettings();
			settings.OriginatorSettings.Add(partitionSettings);

			settings.OriginatorSettings.AddRange(partitionSettings.PartitionSettings);

			// Finally grab a copy of anything that may have been missed
			settings.OriginatorSettings.AddRange(GetSerializableOriginators());
		}

		private IEnumerable<ISettings> GetSerializableOriginators()
		{
			return Originators.GetChildren()
			                  .Where(c => c.Serialize)
			                  .Select(p => p.CopySettings());
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			DisposeOriginators();

			ResetDefaultPermissions();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(KrangCoreSettings settings, IDeviceFactory factory)
		{
			m_LoadedOriginators.Clear();
			factory.OnOriginatorLoaded += FactoryOnOriginatorLoaded;

			try
			{
				base.ApplySettingsFinal(settings, factory);

				// Load services
				factory.LoadOriginators<IService>();

				factory.LoadOriginators<IRoutingGraph>();
				factory.LoadOriginators<IPartitionManager>();
				LoadOriginatorsSkipExceptions(factory);

				// Setup Destination Groups based on DestinationGroupStrings
				IcdHashSet<IDestinationGroup> modifiedDestinationGroups = new IcdHashSet<IDestinationGroup>();
				foreach (IRoutingGraph routingGraph in factory.GetOriginators<IRoutingGraph>())
					modifiedDestinationGroups.AddRange(SetupDestinationGroupsFromDestinationGroupString(routingGraph));
				SetupDestinationGroupsInRooms(modifiedDestinationGroups, factory.GetOriginators<IRoom>());

				ResetDefaultPermissions();
			}
			finally
			{
				factory.OnOriginatorLoaded -= FactoryOnOriginatorLoaded;
			}
		}

		/// <summary>
		/// Gets a free ID for the destination group to use
		/// </summary>
		/// <returns></returns>
		private int GetDestinationGroupId()
		{
			return IdUtils.GetNewId(Originators.GetChildrenIds(), eSubsystem.Destinations);
		}

		private void LoadOriginatorsSkipExceptions(IDeviceFactory factory)
		{
			foreach (int id in factory.GetOriginatorIds())
			{
				try
				{
					// Don't care about the result, loaded originators will be handled by the load event.
					factory.GetOriginatorById(id);
				}
				catch (Exception e)
				{
					Logger.Log(eSeverity.Error, e, "Failed to instantiate {0} with id {1} - {2}",
					           typeof(IOriginator).Name, id, e.Message);
				}
			}
		}

		/// <summary>
		/// Called each time an originator is loaded while applying settings.
		/// </summary>
		/// <param name="factory"></param>
		/// <param name="originator"></param>
		private void FactoryOnOriginatorLoaded(IDeviceFactory factory, IOriginator originator)
		{
			m_LoadedOriginators.Push(originator.Id);
			AddOriginator(originator);

			Logger.Log(eSeverity.Debug, "{0:0.00}% - Finished loading {1}", factory.PercentComplete * 100, originator);
		}

		private void ResetDefaultPermissions()
		{
			PermissionsManager permissionsManager = ServiceProvider.TryGetService<PermissionsManager>();
			if (permissionsManager != null)
				permissionsManager.SetDefaultPermissions(GetPermissions());
		}

		/// <summary>
		/// Override to add actions on StartSettings
		/// This should be used to start communications with devices and perform initial actions
		/// </summary>
		protected override void StartSettingsFinal()
		{
			base.StartSettingsFinal();

			// Start child originators in the order they were loaded
			foreach (int id in m_LoadedOriginators.ToList(m_LoadedOriginators.Count))
				Originators[id].StartSettings();
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in KrangCoreConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			KrangCoreConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in KrangCoreConsole.GetConsoleCommands(this))
				yield return command;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
