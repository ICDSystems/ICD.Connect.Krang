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
	public sealed class SystemKeyManager : IConsoleNode
	{
		private enum eValidationState
		{
			None,
			Invalid,
			Valid,
		}

		private const string PUBLIC_KEY =
			@"MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEXfvikzhAIAOkoqwCXFTpcmr98LJ6CcndaTm+appVLBEo4Evo9c9en0cS8VbmwTWq+8/nnunEIlx4IdildXuNvg==";

		private static readonly string s_RequiredSystemKeyVersion = new Version(1, 0).ToString();

		private License m_SystemKey;
		private eValidationState m_Validation;
		private string m_SystemKeyPath;

		#region Properties

		public string ConsoleName { get { return GetType().Name; } }

		public string ConsoleHelp { get { return "Features for system key registration"; } }

		private static ILoggerService Logger { get { return ServiceProvider.TryGetService<ILoggerService>(); } }

		#endregion

		#region Methods

		/// <summary>
		/// Loads the systemKey at the given path.
		/// </summary>
		/// <param name="path"></param>
		public void LoadSystemKey(string path)
		{
			UnloadSystemKey();

			m_SystemKeyPath = PathUtils.GetProgramConfigPath(path);

			Logger.AddEntry(eSeverity.Informational, "Loading System Key at path {0}", m_SystemKeyPath);

			if (!IcdFile.Exists(m_SystemKeyPath))
			{
				Logger.AddEntry(eSeverity.Error, "Unable to find System Key at path {0}", m_SystemKeyPath);
				return;
			}

			string systemKey = IcdFile.ReadToEnd(m_SystemKeyPath, new UTF8Encoding(false));
			systemKey = EncodingUtils.StripUtf8Bom(systemKey);

			m_SystemKey = License.Load(systemKey);

			m_Validation = ValidateSystemKey(m_SystemKey);
		}

		/// <summary>
		/// Unloads the current active systemKey.
		/// </summary>
		public void UnloadSystemKey()
		{
			m_SystemKey = null;
			m_Validation = eValidationState.None;
		}

		/// <summary>
		/// Returns true if the loaded systemKey was validated.
		/// </summary>
		/// <returns></returns>
		public bool IsValid()
		{
			if (m_SystemKey == null)
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
		/// Validates the loaded system key.
		/// </summary>
		/// <param name="systemKey"></param>
		private eValidationState ValidateSystemKey(License systemKey)
		{
			if (systemKey == null)
				throw new InvalidOperationException("No loaded System Key to validate.");

			IValidationFailure[] validationResults =
				systemKey.Validate()
					   .Signature(PUBLIC_KEY)
					   .And()
					   .AssertThat(ValidateMacAddress,
								   new GeneralValidationFailure
								   {
									   Message = "System Key MAC Address does not match system",
									   HowToResolve = "Are you using this System Key on the correct system?"
								   })
					   .And()
					   .AssertThat(ValidateSystemKeyVersion,
								   new GeneralValidationFailure
								   {
									   Message = string.Format("System Key Version does not match checked version {0}", s_RequiredSystemKeyVersion),
									   HowToResolve = "Are you using this System Key on the correct version of the program?"
								   })
					   .AssertValidLicense()
					   .ToArray();

			foreach (IValidationFailure failure in validationResults)
				Logger.AddEntry(eSeverity.Warning, "{0} - {1} - {2}", GetType().Name, failure.Message, failure.HowToResolve);

			// Only take the system key if it passed validation.
			if (validationResults.Length > 0)
				return eValidationState.Invalid;

			Logger.AddEntry(eSeverity.Informational, "Successfully validated System Key");

			return eValidationState.Valid;
		}

		/// <summary>
		/// Returns true if the mac address in the System Key is valid for this program.
		/// </summary>
		/// <param name="systemKey"></param>
		/// <returns></returns>
		private static bool ValidateMacAddress(License systemKey)
		{
			if (!systemKey.AdditionalAttributes.Contains(KrangSystemKeyAttributes.MAC_ADDRESS))
				return true;

			string macAddress = systemKey.AdditionalAttributes.Get(KrangSystemKeyAttributes.MAC_ADDRESS);
			return IcdEnvironment.MacAddresses.Any(m => CompareMacAddresses(m, macAddress));
		}

		private static bool CompareMacAddresses(string a, string b)
		{
			a = StandardizeMacAddress(a);
			b = StandardizeMacAddress(b);

			return a.Equals(b, StringComparison.OrdinalIgnoreCase);
		}

	    private static string StandardizeMacAddress(string str)
	    {
	        return Regex.Replace(str, "[^0-9A-Fa-f]", "").ToUpper();
	    }

	    /// <summary>
		/// Returns true if the system key version is valid for this version of the program.
		/// </summary>
		/// <param name="systemKey"></param>
		/// <returns></returns>
		private static bool ValidateSystemKeyVersion(License systemKey)
		{
		    if (systemKey.AdditionalAttributes.Contains(KrangSystemKeyAttributes.SYSTEM_KEY_VERSION))
		    {
			    string systemKeyVersion = systemKey.AdditionalAttributes.Get(KrangSystemKeyAttributes.SYSTEM_KEY_VERSION);
			    return s_RequiredSystemKeyVersion.Equals(systemKeyVersion);
		    }

			// Backwards compatibility
// ReSharper disable CSharpWarnings::CS0618
			if (systemKey.AdditionalAttributes.Contains(KrangSystemKeyAttributes.LICENSE_VERSION))

			{
				string systemKeyVersion = systemKey.AdditionalAttributes.Get(KrangSystemKeyAttributes.LICENSE_VERSION);
				return s_RequiredSystemKeyVersion.Equals(systemKeyVersion);
			}
// ReSharper restore CSharpWarnings::CS0618

		    return true;
		}

		#endregion

		#region Console

		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new GenericConsoleCommand<string>("LoadSystemKey", "LoadSystemKey <PATH>", p => LoadSystemKey(p));
		}

		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("SystemKey", m_SystemKeyPath);
		}

		#endregion
	}
}
