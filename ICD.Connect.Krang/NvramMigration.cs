using System;
using System.Text;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Services.Logging;

namespace ICD.Connect.Krang
{
	public static class NvramMigration
	{
		private const string NVRAM_FILE = "NVRAM_DEPRECATED";

		/// <summary>
		/// Migrates the contents of NVRAM to the USER directory.
		/// </summary>
		/// <param name="logger"></param>
		public static void Migrate(ILoggerService logger)
		{
			string nvramPath = PathUtils.Join(PathUtils.RootPath, "NVRAM");
			if (!IcdDirectory.Exists(nvramPath))
				return;

			string configPath = PathUtils.RootConfigPath;
			if (configPath == nvramPath)
				return;

			bool migrated = MigrateDirectory(logger, nvramPath, configPath);
			if (migrated)
				CreateNvramDeprecatedFile();
		}

		/// <summary>
		/// Copies all the files and folders from oldDirectory to newDirectory, creating folders if needed.
		/// Does not remove the files/folders at oldDirectory.
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="oldPath"></param>
		/// <param name="newPath"></param>
		private static bool MigrateDirectory(ILoggerService logger, string oldPath, string newPath)
		{
			bool migrated = false;

			// Migrate files
			foreach (string oldFile in IcdDirectory.GetFiles(oldPath))
			{
				string relativePath = IcdPath.GetRelativePath(oldPath, oldFile);
				string newFile = IcdPath.Combine(newPath, relativePath);

				migrated |= MigrateFile(logger, oldFile, newFile);
			}

			// Migrate directories
			foreach (string oldSubdirectory in IcdDirectory.GetDirectories(oldPath))
			{
				string relativePath = IcdPath.GetRelativePath(oldPath, oldSubdirectory);
				string newSubdirectory = IcdPath.Combine(newPath, relativePath);

				migrated |= MigrateDirectory(logger, oldSubdirectory, newSubdirectory);
			}

			return migrated;
		}

		/// <summary>
		/// Copies the file at the old path to the new path.
		/// Does nothing if a file already exists at the new path.
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="oldPath"></param>
		/// <param name="newPath"></param>
		/// <returns></returns>
		private static bool MigrateFile(ILoggerService logger, string oldPath, string newPath)
		{
			if (!IcdFile.Exists(oldPath))
				throw new InvalidOperationException("File does not exist");

			if (IcdFile.Exists(newPath))
				return false;

			string directory = IcdPath.GetDirectoryName(newPath);
			IcdDirectory.CreateDirectory(directory);

			// Copy file
			logger.AddEntry(eSeverity.Informational, "Migrating {0} to {1}", oldPath, newPath);
			IcdFile.Copy(oldPath, newPath);

			return true;
		}

		/// <summary>
		/// Creates a file with info about the NVRAM deprection in the NVRAM folder.
		/// Does not override if file exists.
		/// </summary>
		private static void CreateNvramDeprecatedFile()
		{
			string directory = IcdPath.Combine(PathUtils.RootPath, "NVRAM");
			if (!IcdDirectory.Exists(directory))
				return;

			string deprecationFile = IcdPath.Combine(directory, NVRAM_FILE);
			if (IcdFile.Exists(deprecationFile))
				return;

			string subDirectory = PathUtils.RootConfigPath.Remove(PathUtils.RootPath);

			using (IcdFileStream stream = IcdFile.Create(deprecationFile))
			{
				string data = string.Format("The 'NVRAM' directory has been deprecated in favor of the '{0}' directory",
											subDirectory);
				stream.Write(data, Encoding.UTF8);
			}
		}
	}
}
