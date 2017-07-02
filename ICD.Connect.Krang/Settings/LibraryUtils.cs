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
									 .Where(a => a != null && IsKrangPlugin(a));
		}

		private class FileNameComparer : IEqualityComparer<string>
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
		public static bool Unzip(string path)
		{
			string outputDir = PathUtils.GetPathWithoutExtension(path);

			// Delete the previous output dir, sometimes the unzip operation doesn't seem to overwrite
			if (IcdDirectory.Exists(outputDir))
			{
				try
				{
					IcdDirectory.Delete(outputDir, true);
					ServiceProvider.TryGetService<ILoggerService>()
								   .AddEntry(eSeverity.Informational, "Removed old plugin {0}", outputDir);
				}
				catch (Exception e)
				{
					ServiceProvider.TryGetService<ILoggerService>()
								   .AddEntry(eSeverity.Warning, "Failed to remove old plugin {0} - {1}", outputDir, e.Message);
					return false;
				}
			}

			return IcdZip.Unzip(path, outputDir);
		}

		/// <summary>
		/// Loops over the archives in the lib directories and unzips them.
		/// </summary>
		public static void UnzipLibAssemblies()
		{
			foreach (string path in GetArchivePaths())
			{
				bool result = Unzip(path);

				// Delete the archive so we don't waste time extracting on next load
				if (result)
				{
					IcdFile.Delete(path);
					ServiceProvider.TryGetService<ILoggerService>()
								   .AddEntry(eSeverity.Informational, "Extracted plugin {0}", path);
				}
				else
				{
					ServiceProvider.TryGetService<ILoggerService>().AddEntry(eSeverity.Warning, "Failed to extract plugin {0}", path);
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
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<string> GetAssemblyPaths()
		{
			return s_LibDirectories.SelectMany(d => PathUtils.RecurseFilePaths(d))
								   .Where(IsAssembly);
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
				// Happens with some crestron libraries
#if SIMPLSHARP
			catch (RestrictionViolationException)
			{
				return null;
			}
#endif
			catch (Exception e)
			{
				ServiceProvider.TryGetService<ILoggerService>()
							   .AddEntry(eSeverity.Warning, e, "Failed to load plugin {0} - {1}", path, e.Message);
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
