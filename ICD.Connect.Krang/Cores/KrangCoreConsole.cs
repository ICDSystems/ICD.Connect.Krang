﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.VolumePoints;
using ICD.Connect.Calendaring.CalendarPoints;
using ICD.Connect.Conferencing.ConferencePoints;
using ICD.Connect.Devices;
using ICD.Connect.Partitioning.Commercial.OccupancyPoints;
using ICD.Connect.Partitioning.RoomGroups;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Themes;

namespace ICD.Connect.Krang.Cores
{
	public static class KrangCoreConsole
	{
		private static readonly List<string> s_Authors =
			new List<string>
			{
				"Chris Cameron",
				"Jeff Thompson",
				"Jack Kanarish",
				"Drew Tingen",
				"Brett Fisher",
				"Brett Heroux",
				"Chris VanLuvanee",
				"Austin Noska",
				"Laura Gomez",
				"Reazul Hoque",
				"Greg Gaskill",
				"Tom Stokes"
			};

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(KrangCore instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return ConsoleNodeGroup.KeyNodeMap("Themes", instance.Originators.GetChildren<ITheme>(), p => (uint)p.Id);
			yield return ConsoleNodeGroup.KeyNodeMap("Ports", instance.Originators.GetChildren<IPort>(), p => (uint)p.Id);
			yield return ConsoleNodeGroup.KeyNodeMap("Devices", instance.Originators.GetChildren<IDevice>(), p => (uint)p.Id);
			yield return ConsoleNodeGroup.KeyNodeMap("Rooms", instance.Originators.GetChildren<IRoom>(), p => (uint)p.Id);
			yield return ConsoleNodeGroup.KeyNodeMap("VolumePoints", instance.Originators.GetChildren<IVolumePoint>(), p => (uint)p.Id);
			yield return ConsoleNodeGroup.KeyNodeMap("ConferencePoints", instance.Originators.GetChildren<IConferencePoint>(), p => (uint)p.Id);
			yield return ConsoleNodeGroup.KeyNodeMap("CalendarPoints", instance.Originators.GetChildren<ICalendarPoint>(), p => (uint)p.Id);
			yield return ConsoleNodeGroup.KeyNodeMap("OccupancyPoints", instance.Originators.GetChildren<IOccupancyPoint>(), p => (uint)p.Id);
			yield return ConsoleNodeGroup.KeyNodeMap("RoomGroups", instance.Originators.GetChildren<IRoomGroup>(), p => (uint) p.Id);

			if (instance.RoutingGraph != null)
				yield return instance.RoutingGraph;

			if (instance.PartitionManager != null)
				yield return instance.PartitionManager;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="addRow"></param>
		public static void BuildConsoleStatus(KrangCore instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("Framework", IcdEnvironment.Framework);
			if (IcdEnvironment.Framework != IcdEnvironment.eFramework.Standard)
			{
				addRow("CrestronSeries", IcdEnvironment.CrestronSeries);
				addRow("CrestronRuntimeEnvironment", IcdEnvironment.CrestronRuntimeEnvironment);
			}
			addRow("Theme Count", instance.Originators.GetChildren<ITheme>().Count());
			addRow("Port Count", instance.Originators.GetChildren<IPort>().Count());
			addRow("Device Count", instance.Originators.GetChildren<IDevice>().Count());
			addRow("Room Count", instance.Originators.GetChildren<IRoom>().Count());
			addRow("VolumePoint Count", instance.Originators.GetChildren<IVolumePoint>().Count());
			addRow("ConferencePoint Count", instance.Originators.GetChildren<IConferencePoint>().Count());
			addRow("RoomGroup Count", instance.Originators.GetChildren<IRoomGroup>().Count());
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(KrangCore instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new ConsoleCommand("whoami", "Displays info about Krang", () => PrintKrang(), true);
			yield return new ConsoleCommand("blame", "Whose fault is it?", () => string.Format("It's {0}'s fault!", s_Authors.Random()), true);
		}

		private static string PrintKrang()
		{
			return @"................................................................................
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
................................................................................";
		}
	}
}
