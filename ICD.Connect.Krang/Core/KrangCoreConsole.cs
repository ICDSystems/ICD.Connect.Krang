﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Panels;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Themes;

namespace ICD.Connect.Krang.Core
{
	public static class KrangCoreConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(KrangCore instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return ConsoleNodeGroup.KeyNodeMap("Themes", instance.Originators.GetChildren<ITheme>().OfType<IConsoleNode>(), p => (uint)((ITheme)p).Id);
			yield return ConsoleNodeGroup.KeyNodeMap("Panels", instance.Originators.GetChildren<IPanelDevice>().OfType<IConsoleNode>(), p => (uint)((IPanelDevice)p).Id);
			yield return ConsoleNodeGroup.KeyNodeMap("Devices", instance.Originators.GetChildren<IDevice>().OfType<IConsoleNode>(), p => (uint)((IDevice)p).Id);
			yield return ConsoleNodeGroup.KeyNodeMap("Ports", instance.Originators.GetChildren<IPort>().OfType<IConsoleNode>(), p => (uint)((IPort)p).Id);
			yield return ConsoleNodeGroup.KeyNodeMap("Rooms", instance.Originators.GetChildren<IRoom>().OfType<IConsoleNode>(), p => (uint)((IRoom)p).Id);

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

			addRow("Theme count", instance.Originators.GetChildren<ITheme>().Count());
			addRow("Panel count", instance.Originators.GetChildren<IPanelDevice>().Count());
			addRow("Device count", instance.Originators.GetChildren<IDevice>().Count());
			addRow("Port count", instance.Originators.GetChildren<IPort>().Count());
			addRow("Room count", instance.Originators.GetChildren<IRoom>().Count());
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
