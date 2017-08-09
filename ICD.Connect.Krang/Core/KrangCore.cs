using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Permissions;
using ICD.Common.Properties;
using ICD.Common.Services;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Krang.Partitioning;
using ICD.Connect.Krang.Remote.Broadcast;
using ICD.Connect.Krang.Remote.Direct;
using ICD.Connect.Krang.Routing;
using ICD.Connect.Krang.Settings;
using ICD.Connect.Panels;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Rooms;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Endpoints.Groups;
using ICD.Connect.Routing.Endpoints.Sources;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;
using ICD.Connect.UI;

namespace ICD.Connect.Krang.Core
{
	public sealed class KrangCore : AbstractOriginator<KrangCoreSettings>, ICore, IConsoleNode
	{
		private readonly CoreOriginatorCollection m_Originators;

		private readonly BroadcastManager m_BroadcastManager;
		private readonly DirectMessageManager m_DirectMessageManager;
		private DiscoveryBroadcastHandler m_BroadcastHandler;

		#region Properties

		public IOriginatorCollection<IOriginator> Originators {get { return m_Originators; } } 

		/// <summary>
		/// Gets the name of the node in the console.
		/// </summary>
		public string ConsoleName { get { return "Core"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return string.Empty; } }

		/// <summary>
		/// Gets the routing graph for the program.
		/// </summary>
		[CanBeNull]
		public RoutingGraph RoutingGraph { get { return m_Originators.GetChildren<RoutingGraph>().SingleOrDefault(); } }

		/// <summary>
		/// Gets the partition manager for the program.
		/// </summary>
		[CanBeNull]
		public PartitionManager PartitionManager { get { return m_Originators.GetChildren<PartitionManager>().SingleOrDefault(); } }

		public BroadcastManager BroadcastManager { get { return m_BroadcastManager; } }

		public DirectMessageManager DirectMessageManager { get { return m_DirectMessageManager; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangCore()
		{
			ServiceProvider.AddService<ICore>(this);

			m_Originators = new CoreOriginatorCollection();

			m_BroadcastManager = ServiceProvider.GetService<BroadcastManager>();
			m_DirectMessageManager = ServiceProvider.GetService<DirectMessageManager>();
		}

		#endregion

		#region Methods

		public bool Route(ISource source, IDestination destination, eConnectionType connectionType, int roomId)
		{
			RoutingGraph graph = RoutingGraph;
			if (graph == null)
				throw new InvalidOperationException("No routing graph in core");

			return graph.Route(new RouteOperation
			{
				Id = Guid.NewGuid(),
				Source = source.Endpoint,
				Destination = destination.Endpoint,
				RoomId = roomId,
				ConnectionType = connectionType
			});
		}

		public bool Route(ISource source, IDestinationGroup destinationGroup, eConnectionType connectionType, int roomId)
		{
			RoutingGraph graph = RoutingGraph;
			if (graph == null)
				throw new InvalidOperationException("No routing graph in core");

			List<bool> results = new List<bool>();
			foreach(var destination in destinationGroup.Destinations.Where(graph.Destinations.ContainsChild).Select(d => graph.Destinations.GetChild(d)))
			{
				IDestination destination1 = destination;
#if SIMPLSHARP
				CrestronUtils.SafeInvoke(() => Route(source, destination1, connectionType, roomId));
#else
			    Route(source, destination1, connectionType, roomId);
#endif
			}
			return results.Unanimous(false);
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

		private void SetOriginators(IEnumerable<IOriginator> originators)
		{
			DisposeOriginators();
			m_Originators.SetChildren(originators);
		}

		/// <summary>
		/// Adds the given originator to the cor.
		/// </summary>
		/// <param name="originator"></param>
		private void AddOriginator(IOriginator originator)
		{
			m_Originators.AddChild(originator);
		}

		/// <summary>
		/// Unsubscribes, disposes and clears the devices.
		/// </summary>
		private void DisposeOriginators()
		{
			foreach (IDisposable originator in m_Originators.OfType<IDisposable>())
				originator.Dispose();
			m_Originators.Clear();
		}
		
		#endregion

		#region Settings

		/// <summary>
		/// Copies the current instance properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		void ICore.CopySettings(ICoreSettings settings)
		{
			CopySettings((KrangCoreSettings)settings);
		}

		/// <summary>
		/// Copies the current state of the Core instance.
		/// </summary>
		/// <returns></returns>
		ICoreSettings ICore.CopySettings()
		{
			return CopySettings();
		}

		/// <summary>
		/// Loads settings from disk and updates the Settings property.
		/// </summary>
		public void LoadSettings()
		{
			FileOperations.LoadCoreSettings<KrangCore, KrangCoreSettings>(this);
		}

		/// <summary>
		/// Applies the settings to the Core instance.
		/// </summary>
		/// <param name="settings"></param>
		void ICore.ApplySettings(ICoreSettings settings)
		{
			IDeviceFactory factory = new CoreDeviceFactory(settings);
			ApplySettings((KrangCoreSettings)settings, factory);
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(KrangCoreSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.OriginatorSettings.AddRange(GetSerializableOriginators<IPort>());
			settings.OriginatorSettings.AddRange(GetSerializableOriginators<IDevice>());
			settings.OriginatorSettings.AddRange(GetSerializableOriginators<IPanelDevice>());
			settings.OriginatorSettings.AddRange(GetSerializableOriginators<IRoom>());
			settings.OriginatorSettings.AddRange(GetSerializableOriginators<IUserInterfaceFactory>());

			var routingGraph = RoutingGraph;
			var routingSettings = routingGraph == null || !routingGraph.Serialize
				                      ? new RoutingGraphSettings()
				                      : routingGraph.CopySettings();
			settings.OriginatorSettings.Add(routingSettings);

			settings.OriginatorSettings.AddRange(routingSettings.ConnectionSettings);
			settings.OriginatorSettings.AddRange(routingSettings.StaticRouteSettings);
			settings.OriginatorSettings.AddRange(routingSettings.SourceSettings);
			settings.OriginatorSettings.AddRange(routingSettings.DestinationSettings);
			settings.OriginatorSettings.AddRange(routingSettings.DestinationGroupSettings);

			var partitionManager = PartitionManager;
			var partitionSettings = partitionManager == null || !partitionManager.Serialize
				                        ? new PartitionManagerSettings()
				                        : partitionManager.CopySettings();
			settings.OriginatorSettings.Add(partitionSettings);

			settings.OriginatorSettings.AddRange(partitionSettings.PartitionSettings);
		}

		private IEnumerable<ISettings> GetSerializableOriginators<T>()
			where T : IOriginator
		{
			return m_Originators.GetChildren<T>()
			                    .Where(c => c.Serialize)
			                    .Select(p => p.CopySettings());
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();
			SetOriginators(Enumerable.Empty<IOriginator>());
			ResetDefaultPermissions();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(KrangCoreSettings settings, IDeviceFactory factory)
		{
			factory.OnOriginatorLoaded += AddOriginator;

			try
			{
				base.ApplySettingsFinal(settings, factory);

				factory.LoadOriginators<IRoutingGraph>();
				factory.LoadOriginators();

				if (settings.Broadcast)
				{
					m_BroadcastHandler = new DiscoveryBroadcastHandler();

					m_DirectMessageManager.RegisterMessageHandler(new InitiateConnectionHandler());
					m_DirectMessageManager.RegisterMessageHandler(new ShareDevicesHandler());
					m_DirectMessageManager.RegisterMessageHandler(new CostUpdateHandler());
					m_DirectMessageManager.RegisterMessageHandler(new RequestDevicesHandler());
					m_DirectMessageManager.RegisterMessageHandler(new DisconnectHandler());
					m_DirectMessageManager.RegisterMessageHandler(new RouteDevicesHandler());
				}

				ResetDefaultPermissions();
			}
			finally
			{
				factory.OnOriginatorLoaded -= AddOriginator;
			}
		}

		private void ResetDefaultPermissions()
		{
			var permissionsManager = ServiceProvider.TryGetService<PermissionsManager>();
			if (permissionsManager != null)
			{
				permissionsManager.SetDefaultPermissions(Permissions);
			}
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("UI Factory count", m_Originators.GetChildren<IUserInterfaceFactory>().Count());
			addRow("Panel count", m_Originators.GetChildren<IPanelDevice>().Count());
			addRow("Device count", m_Originators.GetChildren<IDevice>().Count());
			addRow("Port count", m_Originators.GetChildren<IPort>().Count());

			if (RoutingGraph != null)
			{
				addRow("Connection count", RoutingGraph.Connections.Count);
				addRow("Static Routes count", RoutingGraph.StaticRoutes.Count);
			}

			addRow("Room count", m_Originators.GetChildren<IRoom>().Count());
		}

		/// <summary>
		/// Gets the child console node groups.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return ConsoleNodeGroup.KeyNodeMap("UiFactories", m_Originators.GetChildren<IUserInterfaceFactory>().OfType<IConsoleNode>(), p => (uint)((IUserInterfaceFactory)p).Id);
			yield return ConsoleNodeGroup.KeyNodeMap("Panels", m_Originators.GetChildren<IPanelDevice>().OfType<IConsoleNode>(), p => (uint)((IPanelDevice)p).Id);
			yield return ConsoleNodeGroup.KeyNodeMap("Devices", m_Originators.GetChildren<IDevice>().OfType<IConsoleNode>(), p => (uint)((IDevice)p).Id);
			yield return ConsoleNodeGroup.KeyNodeMap("Ports", m_Originators.GetChildren<IPort>().OfType<IConsoleNode>(), p => (uint)((IPort)p).Id);
			yield return ConsoleNodeGroup.KeyNodeMap("Rooms", m_Originators.GetChildren<IRoom>().OfType<IConsoleNode>(), p => (uint)((IRoom)p).Id);

			if (RoutingGraph != null)
				yield return RoutingGraph;

			if (PartitionManager != null)
				yield return PartitionManager;
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("whoami", "Displays info about Krang", () => PrintKrang(), true);
			yield return new GenericConsoleCommand<int, int, eConnectionType, int>("Route", 
				"Routes source to destination. Usage: Route <sourceId> <destId> <connType> <roomId>",
				(a,b,c,d) => RouteConsoleCommand(a,b,c,d));
			yield return new GenericConsoleCommand<int, int, eConnectionType, int>("RouteGroup",
				"Routes source to destination group. Usage: Route <sourceId> <destGrpId> <connType> <roomId>",
				(a,b,c,d) => RouteGroupConsoleCommand(a,b,c,d));
		}

		public string RouteConsoleCommand(int source, int destination, eConnectionType connectionType, int roomId)
		{
			RoutingGraph graph = RoutingGraph;
			if (graph == null)
				throw new InvalidOperationException("Core contains no RoutingGraph");

			string message;
			if (!graph.Sources.ContainsChild(source) || !graph.Destinations.ContainsChild(destination))
				message = "Krang does not contains a source or destination with that id";
			else
			{
				message = Route(graph.Sources.GetChild(source), graph.Destinations.GetChild(destination),
				                connectionType, roomId)
					          ? "Route successful"
					          : "Route failed";
			}
			return message;
		}

		public string RouteGroupConsoleCommand(int source, int destination, eConnectionType connectionType, int roomId)
		{
			RoutingGraph graph = RoutingGraph;
			if (graph == null)
				throw new InvalidOperationException("Core contains no RoutingGraph");

			string message;
			if (!graph.Sources.ContainsChild(source) || !graph.DestinationGroups.ContainsChild(destination))
				message = "Krang does not contains a source or destination group with that id";
			else
			{
				message = Route(graph.Sources.GetChild(source), graph.DestinationGroups.GetChild(destination),
				                connectionType, roomId)
					          ? "Route successful"
					          : "Route failed";
			}
			return message;
		}

		public static string PrintKrang()
		{
			return string.Format(
				//Krang ASCII Art
@"................................................................................
................................................................................
...................................~?IIIIII?~,?.................................
..................................,IIIIIIIIIIII+,...............................
............................,~+?III++III?~:~+III?+..............................
...........................:IIIIII,IIIIIIIIIIII~?????:..........................
......................=IIIIIIIIIIIII,II??~,=IIII?=????+=,.......................
.....................IIIIIIIIIIIIII?IIIIIIIIIII:?????????+:.....................
....................,IIII?IIIIIIIIIIIIIIIIIIIII?????????????,...................
..................:7IIII+IIIIIIIII:IIIIIIIII?=:??????????????+..................
..............:IIIIIIIIIIIII:IIIIIIIIIIIIIIIII+?,?????????????,.................
.............?IIIII+IIIIII~7IIIIIIIII?IIIIIIII+??IIII+????????~.................
............~IIIIIIIIIIIIIIIIIIIII??IIIIIIII?~??IIIIII++++????+.................
...........+IIIIIIIIIIIIIIII+IIIIIIIIIIIIIIII?:IIIIIIII??:+?????+...............
.........~IIIIIIIIIIIIIIIIIIII?IIIIIIIIIIIII??+IIIIIIII++:+:?????:..............
........:IIIIIIIIIIIIIIIIIIIIIIIIII==IIII?=:++?IIIIIIII???????????..............
........I7IIIIIIIIIIIIIIIIIIIIII+IIIIIIIIIII?IIIIIIIII+??+????????..............
.......~I..?IIIIII+IIIIIIIIIIIIIIIII~??II?~?IIIIIIIII++III?????:??..............
........+.7IIIIIIIIIIII=IIII,IIIIIIIIIIIIIIIIIIIIIIIIIIIII?????+:~..............
.........II,IIII:7IIIIIIIIII7,IIIIIIII==+?IIIIIIIIIIIIIII??????+?+I.............
....~:...IIIIIII=IIII,IIIIIIII,IIIIIIIIIIII+IIIIIIIIIIII+?????????,.............
..II??+.:IIIIIIIIIIIIIIIIIIIII:IIIIIIIIIIIIIIIIIIIIIIII+??????????,.............
..++,:,..=?=IIIII?III=IIIIIIII~IIIIIIIII~IIIIIIIIIII+????~+???????++,...........
.=I+?=...I7IIIIIIIII:IIIIIIII::~IIIIIIII:IIIIIII?+????=::+??????????~...........
.,II???,.=IIIIIIIII:IIIIIIIIIIII+IIIII?IIIIIII?IIII+????+?+??????????...........
..+I+???::II?III?I?II,III+,~IIIIIIIII?+IIIIIIII:I????+,??+,+????????+~..........
..,II???,.=7IIIIII~7IIII:++++:~?III=~I???IIII?+??????????+,=?????????:..........
...,II??+,?I+IIIIIII7IIIII,+++~,,???+?,+?+,=?+???=,,=???????:?????????=.........
....~I+???,IIIIIIIIIIIIIII~~I=::.+,?:+??~?+=:,,,,++,~++?????=???????=??,........
....,III+??7IIIIIIIIIIIIIII7II~~??I~?????+??,++++,?????????:???????=.I+,........
....~IIIIII?+IIIIIIIII:IIIIIIIIIIIIII???I??~++~~=+??????????????????+...........
....:IIIIIII+,I~~?III,,~:::~,=IIIIIIIIIIIII?+~II??+?:????????????+?=............
.....IIIIIIIII,IIIIII,:::,~=+=~::~~~:~:,,:IIIIIII,::~~??????????+?~:............
......=7IIIIIIIIIIIII?:=7,+:+++++~:~++~,::::~:::::::~,?????????????++:..........
.......=IIIIIIIIIIIIIIIIII?++IIIIIIIIIIII?::~?:,:+~,???????????????+???+........
.......?IIIIIIIIIIII?IIIIIIIIIIII+=:,,,?I?????~~=????????+??????????????:.......
........,:~~~IIIIIII?IIIIIII:+=~I+??????~=++:???????+???????????????????=I=.....
...........?IIIIIII???+:IIIIIIIIIIIIIIII????+???????,????,+???????????+???+?,...
.............,IIII?++:,I..~.....:IIIIIII??????????=~+????,.,??+:???+:?:=,+??=...
................,.,.............:I:....,I~.......................::+,::??????...
...............................,I,....,I:...................::+....:??????I7+...
...............................?I..........................??+++??????????+I,...
.............................................................~,+????????I:?.....
.................................................................?,,:+..........
................................................................................");
		}

		#endregion
	}
}
