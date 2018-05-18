using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Licensing;
using ICD.Common.Licensing.Validation;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
#if SIMPLSHARP
using Crestron.SimplSharp.Reflection;
#else
using System.Reflection;
#endif
using System.Text;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Common.Utils.IO;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang
{
	public sealed class LicenseManager : IConsoleNode
	{
		private enum eValidationState
		{
			None,
			Invalid,
			Valid,
		}

		private const string PUBLIC_KEY =
			@"MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEXfvikzhAIAOkoqwCXFTpcmr98LJ6CcndaTm+appVLBEo4Evo9c9en0cS8VbmwTWq+8/nnunEIlx4IdildXuNvg==";

		private static readonly string s_RequiredLicenseVersion = new Version(1, 0).ToString();

		private License m_License;
		private eValidationState m_Validation;
		private string m_LicensePath;

		#region Properties

		public string ConsoleName { get { return GetType().Name; } }

		public string ConsoleHelp { get { return "Features for software license registration"; } }

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

			m_LicensePath = PathUtils.GetProgramConfigPath(path);

			Logger.AddEntry(eSeverity.Informational, "Loading license at path {0}", m_LicensePath);

			if (!IcdFile.Exists(m_LicensePath))
			{
				Logger.AddEntry(eSeverity.Warning, "Unable to find license at path {0}", m_LicensePath);
				return;
			}

			string licenseData = IcdFile.ReadToEnd(m_LicensePath, Encoding.ASCII);

			m_License = License.Load(licenseData);

			m_Validation = ValidateLicense(m_License);
		}

		/// <summary>
		/// Unloads the current active license.
		/// </summary>
		public void UnloadLicense()
		{
			m_License = null;
			m_Validation = eValidationState.None;
		}

		/// <summary>
		/// Returns true if the loaded license was validated.
		/// </summary>
		/// <returns></returns>
		public bool IsValid()
		{
			if (m_License == null)
				return false;

			return m_Validation == eValidationState.Valid;
		}

		/// <summary>
		/// Returns true if the given assembly passes validation.
		/// </summary>
		/// <param name="assembly"></param>
		/// <returns></returns>
		public bool IsValid(Assembly assembly)
		{
			return IsValid();
		}

		/// <summary>
		/// Returns true if the given originator passes validation.
		/// </summary>
		/// <param name="originator"></param>
		/// <returns></returns>
		public bool IsValid(IOriginator originator)
		{
			return IsValid();
		}

		#endregion

		#region Validation

		/// <summary>
		/// Validates the loaded license.
		/// </summary>
		/// <param name="license"></param>
		private eValidationState ValidateLicense(License license)
		{
			if (license == null)
				throw new InvalidOperationException("No loaded license to validate.");

			IValidationFailure[] validationResults =
				license.Validate()
					   .Signature(PUBLIC_KEY)
					   .And()
					   .AssertThat(ValidateMacAddress,
								   new GeneralValidationFailure
								   {
									   Message = "License MAC Address does not match system",
									   HowToResolve = "Are you using this license on the correct system?"
								   })
					   .And()
					   .AssertThat(ValidateLicenseVersion,
								   new GeneralValidationFailure
								   {
									   Message = string.Format("License Version does not match checked version {0}", s_RequiredLicenseVersion),
									   HowToResolve = "Are you using this license on the correct version of the program?"
								   })
					   .AssertValidLicense()
					   .ToArray();

			foreach (IValidationFailure failure in validationResults)
				Logger.AddEntry(eSeverity.Warning, "{0} - {1} - {2}", GetType().Name, failure.Message, failure.HowToResolve);

			// Only take the license if it passed validation.
			if (validationResults.Length > 0)
				return eValidationState.Invalid;

			Logger.AddEntry(eSeverity.Informational, "Successfully validated license");

			return eValidationState.Valid;
		}

		/// <summary>
		/// Returns true if the mac address in the license is valid for this program.
		/// </summary>
		/// <param name="license"></param>
		/// <returns></returns>
		private static bool ValidateMacAddress(License license)
		{
			if (!license.AdditionalAttributes.Contains(KrangLicenseAttributes.MAC_ADDRESS))
				return true;

			string macAddress = StandardizeMacAddress(license.AdditionalAttributes.Get(KrangLicenseAttributes.MAC_ADDRESS));
			return IcdEnvironment.MacAddresses.Any(m => StandardizeMacAddress(macAddress).Equals(m, StringComparison.OrdinalIgnoreCase));
		}

	    private static string StandardizeMacAddress(string str)
	    {
	        return Regex.Replace(str, "[^0-9A-Fa-f]", "").ToUpper();
	    }

	    /// <summary>
		/// Returns true if the license version is valid for this version of the program.
		/// </summary>
		/// <param name="license"></param>
		/// <returns></returns>
		private static bool ValidateLicenseVersion(License license)
		{
			if (!license.AdditionalAttributes.Contains(KrangLicenseAttributes.LICENSE_VERSION))
				return true;

			string licenseVersion = license.AdditionalAttributes.Get(KrangLicenseAttributes.LICENSE_VERSION);
			return s_RequiredLicenseVersion.Equals(licenseVersion);
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
			addRow("License", m_LicensePath);
		}

		#endregion
	}
}
