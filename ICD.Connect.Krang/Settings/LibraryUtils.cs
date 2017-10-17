using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICD.Common.Attributes;
using ICD.Common.Properties;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
#if SIMPLSHARP
using Crestron.SimplSharp.Reflection;
#else
using System.Reflection;
#endif

namespace ICD.Connect.Krang.Settings
{
	/// <summary>
	/// Utility methods for working with reflection.
	/// </summary>
	public static class LibraryUtils
	{
		private const string DLL_EXT = ".dll";
		private const string VERSION_MATCH = @"-[v|V]([\d+.?]+\d)$";

		private static readonly string[] s_ArchiveExtensions =
		{
			".CPZ",
			".CLZ",
			".CPLZ"
		};

		private static readonly string[] s_LibDirectories;

		private static ILoggerService Logger { get { return ServiceProvider.TryGetService<ILoggerService>(); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		static LibraryUtils()
		{
			s_LibDirectories = new[]
			{
				IcdDirectory.GetApplicationDirectory(),
				PathUtils.ProgramLibPath,
				PathUtils.CommonLibPath
			};
		}

		#region Methods

		/// <summary>
		/// Gets assemblies with the KrangPlugin assembly attribute
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<Assembly> GetPluginAssemblies()
		{
			UnzipLibAssemblies();

			return GetAssemblyPaths().OrderBy<string, int>(GetDirectoryIndex)
									 .ThenByDescending<string, Version>(GetAssemblyVersionFromPath)
									 .Distinct(new FileNameComparer())
									 .Select<string, Assembly>(SafeLoadAssembly)
									 .Where(a => a != null && IsKrangPlugin(a))
									 .OrderBy(a => a.FullName);
		}

		private sealed class FileNameComparer : IEqualityComparer<string>
		{
			public bool Equals(string x, string y)
			{
				return GetHashCode(x) == GetHashCode(y);
			}

			public int GetHashCode(string obj)
			{
				return obj == null ? 0 : IcdPath.GetFileName(obj).GetHashCode();
			}
		}

		/// <summary>
		/// Unzips the archive at the given path.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="message"></param>
		public static bool Unzip(string path, out string message)
		{
			string outputDir = PathUtils.GetPathWithoutExtension(path);

			// Delete the previous output dir, sometimes the unzip operation doesn't seem to overwrite
			if (IcdDirectory.Exists(outputDir))
			{
				try
				{
					IcdDirectory.Delete(outputDir, true);
					Logger.AddEntry(eSeverity.Informational, "Removed old plugin {0}", outputDir);
				}
				catch (Exception e)
				{
					message = string.Format("Failed to remove old plugin {0} - {1}", outputDir, e.Message);
					return false;
				}
			}

			return IcdZip.Unzip(path, outputDir, out message);
		}

		/// <summary>
		/// Loops over the archives in the lib directories and unzips them.
		/// </summary>
		public static void UnzipLibAssemblies()
		{
			foreach (string path in GetArchivePaths().Where(p => !IsProgramCpz(p)))
			{
				string message;
				bool result = Unzip(path, out message);

				// Delete the archive so we don't waste time extracting on next load
				if (result)
				{
					IcdFile.Delete(path);
					Logger.AddEntry(eSeverity.Informational, "Extracted plugin {0}", path);
				}
				else
				{
					Logger.AddEntry(eSeverity.Warning, "Failed to extract plugin {0} - {1}", path, message);
				}
			}
		}

		#endregion

		#region Private Methods

		private static bool IsKrangPlugin(Assembly assembly)
		{
			return ReflectionUtils.GetCustomAttributes<KrangPluginAttribute>(assembly).Any();
		}

		/// <summary>
		/// Gets the paths to the available runtime assemblies.
		/// Assemblies may be located in 3 places, in order of importance:
		///		Program installation directory
		///		Program configuration
		///		Common configuration
		/// 
		/// Further, for the sake of convenience, check if this is running from a build directory
		/// and check sibling build directories for assemblies.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<string> GetAssemblyPaths()
		{
			return GetBuildAssemblyPaths().Concat(GetLibAssemblyPaths());
		}

		/// <summary>
		/// Gets the paths to the available runtime assemblies.
		/// Assemblies may be located in 3 places, in order of importance:
		///		Program installation directory
		///		Program configuration
		///		Common configuration
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<string> GetLibAssemblyPaths()
		{
			return s_LibDirectories.SelectMany(d => PathUtils.RecurseFilePaths(d)).Where(IsAssembly);
		}

		/// <summary>
		/// If the program is being run from a build directory, finds assemblies from sibling projects
		/// that are part of the same solution.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<string> GetBuildAssemblyPaths()
		{
			string path = typeof(LibraryUtils)
#if SIMPLSHARP
				.GetCType()
#else
				.GetTypeInfo()
#endif
				.Assembly.GetPath();

			// Find the .sln
			while (path != null)
			{
				path = IcdPath.GetDirectoryName(path);
				bool foundSln = !string.IsNullOrEmpty(path) && IcdDirectory
									.GetFiles(path).Any(p => IcdPath.GetExtension(p).Equals(".sln", StringComparison.OrdinalIgnoreCase));

				if (foundSln)
					return PathUtils.RecurseFilePaths(path)
					                .Where(p => p.Contains("bin"))
					                .Where(IsAssembly);
			}

			return Enumerable.Empty<string>();
		}

		/// <summary>
		/// Gets the paths to archives stored in lib directories.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<string> GetArchivePaths()
		{
			return s_LibDirectories.SelectMany(d => PathUtils.RecurseFilePaths(d))
								   .Where(IsArchive);
		}

		/// <summary>
		/// Returns true if the given path represents a .CPZ file in a program directory.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static bool IsProgramCpz(string path)
		{
			return IcdPath.GetExtension(path).Equals(".CPZ", StringComparison.OrdinalIgnoreCase) &&
			       path.Contains(IcdDirectory.GetApplicationDirectory());
		}

		/// <summary>
		/// Returns true if the file at the given path is an assembly.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static bool IsAssembly(string path)
		{
			return string.Equals(IcdPath.GetExtension(path), DLL_EXT, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Returns true if the file at the given path is an archive.
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		private static bool IsArchive(string arg)
		{
			return s_ArchiveExtensions.Any(e => e.Equals(IcdPath.GetExtension(arg), StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Returns the LibDirectories index for the given path.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static int GetDirectoryIndex(string path)
		{
			return s_LibDirectories.FindIndex(p => path.ToLower().StartsWith(p.ToLower()));
		}

		/// <summary>
		/// Attempts to load the assembly at the given path. Returns null if an exception is caught.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		[CanBeNull]
		private static Assembly SafeLoadAssembly(string path)
		{
			try
			{
				return ReflectionUtils.LoadAssemblyFromPath(path);
			}
#if SIMPLSHARP
			// Happens with some crestron libraries
			catch (RestrictionViolationException)
			{
				return null;
			}
#endif
			catch (Exception e)
			{
				Logger.AddEntry(eSeverity.Warning, e, "Failed to load plugin {0} - {1}", path, e.Message);
				return null;
			}
		}

		/// <summary>
		/// Gets the version from the path.
		/// e.g. ICD.SimplSharp.Common returns 0.0.0.0
		///	     ICD.SimplSharp.Common-V1.0 returns 1.0.0.0
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static Version GetAssemblyVersionFromPath(string path)
		{
			string filename = IcdPath.GetFileNameWithoutExtension(path);

			Regex regex = new Regex(VERSION_MATCH);
			Match match = regex.Match(filename);

			return match.Success ? new Version(match.Groups[1].Value) : new Version(0, 0);
		}

#endregion
	}
}
