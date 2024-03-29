﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Services;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Panels.Devices;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Cores;
using ICD.Connect.Settings.Originators;
#if SIMPLSHARP
using Crestron.SimplSharp.Reflection;
#else
using System.Reflection;
#endif

namespace ICD.Connect.Krang.Cores
{
	public static class KrangBootstrapConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(KrangBootstrap instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return instance.Krang;
			yield return instance.BroadcastManager;
			yield return instance.DirectMessageManager;

			yield return ConsoleNodeGroup.IndexNodeMap("Services", ServiceProvider.GetServices().OfType<IConsoleNodeBase>().OrderBy(s => s.GetType().Name));
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="addRow"></param>
		public static void BuildConsoleStatus(KrangBootstrap instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("Core", instance.Krang);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(KrangBootstrap instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new ConsoleCommand("LoadCore", "Loads and applies the XML config.", () => LoadSettings(instance));
			yield return new ConsoleCommand("SaveCore", "Saves the current settings to XML.", () => SaveSettings(instance));
			yield return new ConsoleCommand("RebuildCore", "Rebuilds the core using the current settings.", () => RebuildCore(instance));
			yield return new ConsoleCommand("PrintTypes", "Prints the loaded device types.", () => PrintTypes());
			yield return new ConsoleCommand("VersionInfo", "Prints version information for the loaded assemblies.", () => PrintVersions());
			yield return new ConsoleCommand("Health", "Prints an overview of the current status of the loaded system.", () => PrintHealth(instance));
		}

		private static void LoadSettings(KrangBootstrap instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");
#if LICENSING
			if (!instance.SystemKeyManager.IsValid())
				return;
#endif
				instance.Krang.LoadSettings(null);
		}

		private static void SaveSettings(KrangBootstrap instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			// Saving settings involves running some console commands to get processor information.
			// Executing console commands from a console command thread is extremely slow.
			ThreadingUtils.SafeInvoke(() =>
			{
				ICoreSettings settings = instance.Krang.CopySettings();
				FileOperations.SaveSettings(settings);
			});
		}

		private static void RebuildCore(KrangBootstrap instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			FileOperations.LoadCoreSettings(instance.Krang, instance.Krang.CopySettings(), null);
		}

		private static string PrintVersions()
		{
			TableBuilder builder = new TableBuilder("Assembly", "Path", "Info Ver.", "Assembly Ver.", "Date");

			foreach (Assembly assembly in PluginFactory.GetFactoryAssemblies().OrderBy(a => a.FullName))
			{
				string name = assembly.GetName().Name;
				string path = assembly.GetPath();
				string infoVersion;
				assembly.TryGetInformationalVersion(out infoVersion);
				string assemblyVersion = assembly.GetName().Version.ToString();
				string date = path == null ? null : IcdFile.GetLastWriteTime(path).ToString();

				path = path == null ? null : IcdPath.GetDirectoryName(path);

				builder.AddRow(name, path, infoVersion, assemblyVersion, date);
			}

			return builder.ToString();
		}

		private static string PrintTypes()
		{
			TableBuilder builder = new TableBuilder("Type", "Assembly", "Path", "Version", "Date");

			foreach (string factoryName in PluginFactory.GetFactoryNames().Order())
			{
				Assembly assembly = PluginFactory.GetType(factoryName).GetAssembly();

				string name = assembly.GetName().Name;
				string path = assembly.GetPath();
				string version = assembly.GetName().Version.ToString();
				string date = path == null ? null : IcdFile.GetLastWriteTime(path).ToString();

				path = path == null ? null : IcdPath.GetDirectoryName(path);

				builder.AddRow(factoryName, name, path, version, date);
			}

			return builder.ToString();
		}

		private static string PrintHealth([NotNull] KrangBootstrap instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			TableBuilder builder = new TableBuilder("Item", "ID", "Online", "LifetimeState", "Firmware");

			Action<IDeviceBase> addRow = d =>
			{
				string name = string.IsNullOrEmpty(d.CombineName) ? d.Name : d.CombineName;
				string id = d.Id.ToString();
				string onlineColor = d.IsOnline ? AnsiUtils.COLOR_GREEN : AnsiUtils.COLOR_RED;
				string online = AnsiUtils.Format(d.IsOnline.ToString(), onlineColor);
				string stateColor = d.LifecycleState == eLifecycleState.Started
					                    ? AnsiUtils.COLOR_GREEN
					                    : d.LifecycleState == eLifecycleState.Disposed
										? AnsiUtils.COLOR_RED : AnsiUtils.COLOR_YELLOW;
				string state = AnsiUtils.Format(d.LifecycleState.ToString(), stateColor);
				var device = d as IDevice;
				string firmware = device != null && device.MonitoredDeviceInfo.FirmwareVersion != null
					                  ? device.MonitoredDeviceInfo.FirmwareVersion.ToString()
					                  : null;

				builder.AddRow(name, id, online, state, firmware);
			};

			builder.AddRow("-Panels-", null, null, null, null);
			foreach (IPanelDevice panel in instance.Krang.Originators.GetChildren<IPanelDevice>())
				addRow(panel);

			builder.AddEmptyRow();

			builder.AddRow("-Ports-", null, null, null, null);
			foreach (IPort port in instance.Krang.Originators.GetChildren<IPort>())
				addRow(port);

			builder.AddEmptyRow();

			builder.AddRow("-Devices-", null, null, null, null);
			foreach (IDevice device in instance.Krang.Originators.GetChildren<IDevice>().Where(d => !(d is IPanelDevice)))
				addRow(device);

			return builder.ToString();
		}
	}
}
