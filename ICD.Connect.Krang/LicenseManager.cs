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
using ICD.Common.Utils;
using ICD.Common.Utils.IO;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang
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

			m_LicensePath = PathUtils.GetProgramConfigPath(path);

			Logger.AddEntry(eSeverity.Informational, "Loading license at path {0}", m_LicensePath);

			if (!IcdFile.Exists(m_LicensePath))
			{
				Logger.AddEntry(eSeverity.Warning, "Unable to find license at path {0}", m_LicensePath);
				return;
			}

			string licenseData = IcdFile.ReadToEnd(m_LicensePath, Encoding.ASCII);
			License license = License.Load(licenseData);
			IValidationFailure[] validationResults = license.Validate()
			                                                .Signature(PublicKey)
			                                                .And()
			                                                .AssertThat(ValidateMacAddress,
			                                                            new GeneralValidationFailure
			                                                            {
				                                                            Message = "License MAC Address does not match system",
				                                                            HowToResolve =
					                                                            "Are you using this license on the correct system?"
			                                                            })
			                                                .And()
			                                                .AssertThat(ValidateProgramSlot,
			                                                            new GeneralValidationFailure
			                                                            {
				                                                            Message = "License Program Slot does not match system",
				                                                            HowToResolve =
					                                                            "Are you using this license on the correct slot?"
			                                                            })
			                                                .AssertValidLicense()
			                                                .ToArray();

			foreach (IValidationFailure failure in validationResults)
				Logger.AddEntry(eSeverity.Warning, "{0} - {1} - {2}", GetType().Name, failure.Message, failure.HowToResolve);

			// Only take the license if it passed validation.
			if (validationResults.Length > 0)
				return;

			Logger.AddEntry(eSeverity.Informational, "Successfully Loaded license");
			m_License = license;
		}

		/// <summary>
		/// Returns true if the program slot in the license is valid for this program.
		/// </summary>
		/// <param name="license"></param>
		/// <returns></returns>
		private bool ValidateProgramSlot(License license)
		{
			if (!license.AdditionalAttributes.Contains("ProgramSlot"))
				return true;

			string slotString = license.AdditionalAttributes.Get("ProgramSlot");

			uint programSlot;
			if (!StringUtils.TryParse(slotString, out programSlot))
				return false;

			return programSlot == ProgramUtils.ProgramNumber;
		}

		/// <summary>
		/// Returns true if the mac address in the license is valid for this program.
		/// </summary>
		/// <param name="license"></param>
		/// <returns></returns>
		private bool ValidateMacAddress(License license)
		{
			if (!license.AdditionalAttributes.Contains("MacAddress"))
				return true;

			string macAddress = license.AdditionalAttributes.Get("MacAddress");
			return IcdEnvironment.MacAddresses.Any(m => macAddress.Equals(m, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Unloads the current active license.
		/// </summary>
		public void UnloadLicense()
		{
			m_License = null;
			m_PublicKey = null;
		}

		/// <summary>
		/// Returns true if the loaded license was validated.
		/// </summary>
		/// <returns></returns>
		public bool IsValid()
		{
			return m_License != null;
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
