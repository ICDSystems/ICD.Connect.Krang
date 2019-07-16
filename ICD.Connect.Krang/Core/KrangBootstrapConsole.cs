using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Services;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Cores;
#if SIMPLSHARP
using Crestron.SimplSharp.Reflection;
#else
using System.Reflection;
#endif

namespace ICD.Connect.Krang.Core
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
			yield return instance.LicenseManager;
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
			yield return new ConsoleCommand("PrintPlugins", "Prints the loaded plugin assemblies.", () => PrintPlugins());
			yield return new ConsoleCommand("PrintTypes", "Prints the loaded device types.", () => PrintTypes());
			yield return new ConsoleCommand("VersionInfo", "Prints version information for the loaded assemblies.", () => PrintVersionInfo());
		}

		private static void LoadSettings(KrangBootstrap instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");
#if LICENSING
			if (instance.LicenseManager.IsValid())
#endif
				instance.Krang.LoadSettings();
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

			FileOperations.ApplyCoreSettings(instance.Krang, instance.Krang.CopySettings());
		}

		private static string PrintPlugins()
		{
			TableBuilder builder = new TableBuilder("Assembly", "Path", "Version", "Date");

			foreach (Assembly assembly in PluginFactory.GetFactoryAssemblies().OrderBy(a => a.FullName))
			{
				string name = assembly.GetName().Name;
				string path = assembly.GetPath();
				string version = assembly.GetName().Version.ToString();
				DateTime date = IcdFile.GetLastWriteTime(path);

				path = IcdPath.GetDirectoryName(path);

				builder.AddRow(name, path, version, date);
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
				DateTime date = IcdFile.GetLastWriteTime(path);

				path = IcdPath.GetDirectoryName(path);

				builder.AddRow(factoryName, name, path, version, date);
			}

			return builder.ToString();
		}
		
		private static string PrintVersionInfo()
		{
			TableBuilder builder = new TableBuilder("Assembly", "Path", "Informational Version", "Assembly Version", "Date");

			foreach (Assembly assembly in PluginFactory.GetFactoryAssemblies().OrderBy(a => a.FullName))
			{
				string name = assembly.GetName().Name;
				string path = assembly.GetPath();
				string version = assembly.GetName().Version.ToString();
				DateTime date = IcdFile.GetLastWriteTime(path);

				string infoVersion;
				assembly.TryGetInformationalVersion(out infoVersion);

				path = IcdPath.GetDirectoryName(path);

				builder.AddRow(name, path, infoVersion, version, date);
			}

			return builder.ToString();
		}
	}
}
