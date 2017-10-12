using System.Text;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Xml;
using ICD.Connect.Krang.Core;
using ICD.Connect.Krang.Settings.Migration;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;
#if SIMPLSHARP
using Crestron.SimplSharp.Reflection;
#else
using System;
#endif

namespace ICD.Connect.Krang.Settings
{
	/// <summary>
	/// Methods for handling serialization and deserialization of settings.
	/// </summary>
	public static class FileOperations
	{
		private const string CONFIG_LOCAL_PATH = "RoomConfig-Base.xml";

		public static string IcdConfigPath { get { return PathUtils.GetProgramConfigPath(CONFIG_LOCAL_PATH); } }

		public static ILoggerService Logger { get { return ServiceProvider.TryGetService<ILoggerService>(); } }

		/// <summary>
		/// Applies the settings to Krang.
		/// </summary>
		public static void ApplyCoreSettings<TCore, TSettings>(TCore core, TSettings settings)
			where TSettings : ICoreSettings
			where TCore : ICore
		{

			Logger.AddEntry(eSeverity.Notice, "Applying settings");

			IDeviceFactory factory = new CoreDeviceFactory(settings);
			core.ApplySettings(settings, factory);

			Logger.AddEntry(eSeverity.Notice, "Finished applying settings");
		}

		/// <summary>
		/// Serializes the settings to disk.
		/// </summary>
		/// <param name="settings"></param>
		public static void SaveSettings(ISettings settings)
		{
			SaveSettings(settings, false);
		}

		/// <summary>
		/// Serializes the settings to disk.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="backup"></param>
		public static void SaveSettings(ISettings settings, bool backup)
		{
			if (backup)
				BackupSettings();

			string path = IcdConfigPath;
			string directory = IcdPath.GetDirectoryName(path);
			IcdDirectory.CreateDirectory(directory);

			Logger.AddEntry(eSeverity.Notice, "Saving settings to {0}", path);

			using (IcdFileStream stream = IcdFile.OpenWrite(path))
			{
				using (IcdXmlTextWriter writer = new IcdXmlTextWriter(stream, new UTF8Encoding(false)))
				{
					WriteSettingsWarning(writer);
					settings.ToXml(writer);
				}
			}

			Logger.AddEntry(eSeverity.Notice, "Finished saving settings");
		}

		/// <summary>
		/// Copies the existing settings to a new path with the current date.
		/// </summary>
		public static void BackupSettings()
		{
			if (!IcdFile.Exists(IcdConfigPath))
				return;

			string name = IcdPath.GetFileNameWithoutExtension(IcdConfigPath);
			string date = IcdEnvironment.GetLocalTime().ToString("MM-dd-yyyy_HH-mm");
			string newName = string.Format("{0}_Backup_{1}", name, date);
			string newPath = PathUtils.ChangeFilenameWithoutExt(IcdConfigPath, newName);

			Logger.AddEntry(eSeverity.Notice, "Creating settings backup of {0} at {1}", IcdConfigPath, newPath);

			IcdFile.Copy(IcdConfigPath, newPath);

			Logger.AddEntry(eSeverity.Notice, "Finished settings backup");
		}

		/// <summary>
		/// Writes a comment to the xml warning integrators about this XML being overwritten
		/// </summary>
		/// <param name="writer"></param>
		private static void WriteSettingsWarning(IcdXmlTextWriter writer)
		{
			writer.WriteComment("\nThis configuration is generated automatically.\n" +
			                    "Only change this file if you know what you are doing.\n" +
			                    "Any invalid data, whitespace, and comments will be deleted the next time this is generated.\n");
		}

		/// <summary>
		/// Loads the settings from disk to the core.
		/// </summary>
		public static void LoadCoreSettings<TCore, TSettings>(TCore core)
			where TSettings : ICoreSettings, new()
			where TCore : ICore
		{
			TSettings settings = Activator.CreateInstance<TSettings>();

			// Ensure the new core settings don't default to an id of 0.
			settings.Id = 1;

			LoadCoreSettings(core, settings);
		}

		/// <summary>
		/// Loads the settings from disk to the core.
		/// </summary>
		public static void LoadCoreSettings<TCore, TSettings>(TCore core, TSettings settings)
			where TSettings : ICoreSettings
			where TCore : ICore
		{
			string path = IcdConfigPath;

			Logger.AddEntry(eSeverity.Notice, "Loading settings from {0}", path);

			// Load XML config into string
			string configXml = null;
			if (IcdFile.Exists(path))
				configXml = IcdFile.ReadToEnd(path, Encoding.UTF8);

			bool save = false;

			if (!string.IsNullOrEmpty(configXml))
			{
				// TODO Temporary - V5 release
				if (LegacySettingsMigration.IsSingleRoom(configXml))
				{
				    configXml = LegacySettingsMigration.Migrate(configXml);
				    save = true;
				}

				// TODO Temporary - adding routing element to existing configs
				if (!ConnectionsRoutingMigration.HasRoutingElement(configXml))
				{
				    configXml = ConnectionsRoutingMigration.Migrate(configXml);
				    save = true;
				}

				// TODO Temporary - Source.Output and Destination.Input becomes Source.Address and Destination.Address
				SourceDestinationAddressMigration.Migrate(configXml);

				// TODO Temporary - V5 release
				if (!SourceDestinationRoutingMigration.HasSourceOrDestinationRoutingElement(configXml))
				{
				    configXml = SourceDestinationRoutingMigration.Migrate(configXml);
				    save = true;
				}

				// TODO Temporary - Older configs didn't specify a routing graph originator
				if (!RoutingGraphMigration.HasRoutingGraphOriginator(configXml))
				{
					configXml = RoutingGraphMigration.Migrate(configXml);
					save = true;
				}
			}

			Logger.AddEntry(eSeverity.Notice, "Finished loading settings");

			// Save a stub xml file if one doesn't already exist
			if (string.IsNullOrEmpty(configXml))
				save = true;
			else
				settings.ParseXml(configXml);

			if (save)
				SaveSettings(settings, true);

			ApplyCoreSettings(core, settings);
		}
	}
}
