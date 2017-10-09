using System.Collections.Generic;
using System.Linq;
#if SIMPLSHARP
using Crestron.SimplSharp.CrestronIO;
#else
using System.IO;
#endif
using System.Text;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.IO;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using Portable.Licensing;
using Portable.Licensing.Validation;

namespace ICD.Connect.Krang.Settings
{
	public sealed class LicenseManager : IConsoleNode
	{
		private string m_PublicKey;
		private License m_License;
		private string m_LicensePath;

		#region Properties

		public string ConsoleName { get { return GetType().Name; } }

		public string ConsoleHelp { get { return "Features for software license registration"; } }
		private string PublicKey { get { return m_PublicKey ?? (m_PublicKey = LoadPublicKey()); } }

		private static ILoggerService Logger { get { return ServiceProvider.TryGetService<ILoggerService>(); } }

		#endregion

		#region Methods

		/// <summary>
		/// Loads the license at the given path.
		/// </summary>
		/// <param name="path"></param>
		public void LoadLicense(string path)
		{
			UnloadLicense();

			m_LicensePath = PathUtils.GetProgramConfigPath("License", path);
			if (!IcdFile.Exists(m_LicensePath))
				throw new FileNotFoundException(string.Format("Unable to load license at path {0}", m_LicensePath));

			string licenseData = IcdFile.ReadToEnd(m_LicensePath, Encoding.ASCII);
			m_License = License.Load(licenseData);
			IValidationFailure[] validationResults = m_License.Validate()
			                                                  .Signature(PublicKey)
			                                                  .AssertValidLicense()
			                                                  .ToArray();

			foreach (IValidationFailure failure in validationResults)
				Logger.AddEntry(eSeverity.Warning, "{0} - {1} - {2}", GetType().Name, failure.Message, failure.HowToResolve);
		}

		/// <summary>
		/// Unloads the current active license.
		/// </summary>
		public void UnloadLicense()
		{
			m_License = null;
			m_PublicKey = null;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the contents of the publickey file from disk.
		/// </summary>
		/// <returns></returns>
		private string LoadPublicKey()
		{
			string publicKeyPath = PathUtils.Join(IcdDirectory.GetApplicationDirectory(), "licensekey");
			return IcdFile.ReadToEnd(publicKeyPath, Encoding.ASCII);
		}

		#endregion

		#region Console

		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new GenericConsoleCommand<string>("LoadLicense", "LoadLicense <PATH>", p => LoadLicense(p));
		}

		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Public Key", m_PublicKey);
			addRow("License", m_LicensePath);
		}

		#endregion
	}
}
