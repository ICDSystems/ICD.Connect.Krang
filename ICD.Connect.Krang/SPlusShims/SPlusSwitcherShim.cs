using ICD.Connect.Settings.Originators;
#if SIMPLSHARP
using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.SPlus;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.SPlusShims
{
	[PublicAPI("SPlus")]
	public sealed class SPlusSwitcherShim : IDisposable
	{
		#region S+ Delegates

		public delegate ushort SPlusGetSignalDetectedState(ushort input);

		public delegate ushort SPlusGetInputCount();

		public delegate ushort SPlusGetOutputCount();

		public delegate ushort SPlusGetInputForOutput(ushort output);

		public delegate ushort SPlusRoute(ushort input, ushort output);

		public delegate ushort SPlusClearOutput(ushort output);

		#endregion

		#region S+ Events

		[PublicAPI("SPlus")]
		public SPlusGetSignalDetectedState GetSignalDetectedStateCallback { get; set; }

		[PublicAPI("SPlus")]
		public SPlusGetInputCount GetInputCountCallback { get; set; }

		[PublicAPI("SPlus")]
		public SPlusGetOutputCount GetOutputCountCallback { get; set; }

		[PublicAPI("SPlus")]
		public SPlusGetInputForOutput GetInputForOutputCallback { get; set; }

		[PublicAPI("SPlus")]
		public SPlusRoute RouteCallback { get; set; }

		[PublicAPI("SPlus")]
		public SPlusClearOutput ClearOutputCallback { get; set; }

		#endregion

		private ushort m_SwitcherId;

		[PublicAPI("SPlus")]
		public event EventHandler OnSettingsApplied;

		private SPlusSwitcherControl m_SwitcherControl;

		/// <summary>
		/// Constructor.
		/// </summary>
		public SPlusSwitcherShim()
		{
			SPlusKrangBootstrap.OnKrangLoaded += SPlusKrangBootstrapOnKrangLoaded;
		}

		private void SPlusKrangBootstrapOnKrangLoaded(object sender, EventArgs eventArgs)
		{
			SetSwitcher(m_SwitcherId);
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnSettingsApplied = null;
			SPlusKrangBootstrap.OnKrangLoaded -= SPlusKrangBootstrapOnKrangLoaded;

			SetSwitcher(0);
		}

		/// <summary>
		/// Sets the wrapped switcher.
		/// </summary>
		/// <param name="id"></param>
		[PublicAPI("SPlus")]
		public void SetSwitcher(ushort id)
		{
			m_SwitcherId = id;
			Unsubscribe(m_SwitcherControl);
			m_SwitcherControl = GetSwitcherControl(id);
			Subscribe(m_SwitcherControl);
		}

		/// <summary>
		/// Triggers a source detection check for the given input.
		/// </summary>
		/// <param name="input"></param>
		[PublicAPI("SPlus")]
		public void UpdateSourceDetection(ushort input)
		{
			if (m_SwitcherControl == null)
				return;

			m_SwitcherControl.UpdateSourceDetection(input, eConnectionType.Video | eConnectionType.Audio);
		}

		/// <summary>
		/// Triggers an input routing check for the given output.
		/// </summary>
		/// <param name="output"></param>
		[PublicAPI("SPlus")]
		public void UpdateSwitcherOutput(ushort output)
		{
			if (m_SwitcherControl == null)
				return;

			ushort? input = GetInputForOutputCallback == null ? (ushort?)null : GetInputForOutputCallback(output);

			if (input == null)
				m_SwitcherControl.ClearOutput(output, eConnectionType.Video | eConnectionType.Audio);
			else
				m_SwitcherControl.SetInputForOutput(output, (int)input, eConnectionType.Video | eConnectionType.Audio);
		}

		/// <summary>
		/// Sets the online state of the current switcher device.
		/// </summary>
		/// <param name="online"></param>
		[PublicAPI("SPlus")]
		public void SetDeviceOnline(ushort online)
		{
			if (m_SwitcherControl == null)
				return;

			m_SwitcherControl.Parent.SetOnlineStatus(online != 0);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the switcher control for the device with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[CanBeNull]
		private SPlusSwitcherControl GetSwitcherControl(ushort id)
		{
			IOriginator device;
			if (!SPlusKrangBootstrap.Krang.Originators.TryGetChild(id, out device))
				return null;

			SPlusSwitcher switcher = device as SPlusSwitcher;
			return switcher == null ? null : switcher.Controls.GetControl<SPlusSwitcherControl>();
		}

		#endregion

		#region Control Callbacks

		/// <summary>
		/// Subscribe to the switcher control events.
		/// </summary>
		/// <param name="switcherControl"></param>
		private void Subscribe(SPlusSwitcherControl switcherControl)
		{
			if (switcherControl == null)
				return;

			switcherControl.Parent.OnSettingsApplied += ParentOnSettingsApplied;

			switcherControl.GetSignalDetectedStateCallback = SwitcherControlGetSignalDetectedStateCallback;
			switcherControl.GetInputsCallback = SwitcherControlGetInputsCallback;
			switcherControl.GetOutputsCallback = SwitcherControlGetOutputsCallback;
			switcherControl.RouteCallback = SwitcherControlRouteCallback;
			switcherControl.ClearOutputCallback = SwitcherControlClearOutputCallback;
		}

		/// <summary>
		/// Unsubscribe from the switcher control events.
		/// </summary>
		/// <param name="switcherControl"></param>
		private void Unsubscribe(SPlusSwitcherControl switcherControl)
		{
			if (switcherControl == null)
				return;

			switcherControl.Parent.OnSettingsApplied -= ParentOnSettingsApplied;

			switcherControl.GetSignalDetectedStateCallback = null;
			switcherControl.GetInputsCallback = null;
			switcherControl.GetOutputsCallback = null;
			switcherControl.RouteCallback = null;
			switcherControl.ClearOutputCallback = null;
		}

		/// <summary>
		/// Called when settings are applied on the switcher device.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ParentOnSettingsApplied(object sender, EventArgs eventArgs)
		{
			OnSettingsApplied.Raise(this);
		}

		private bool SwitcherControlClearOutputCallback(int output, eConnectionType type)
		{
			return (ClearOutputCallback((ushort)output) != 0);
		}

		private bool SwitcherControlRouteCallback(RouteOperation info)
		{
			return RouteCallback != null && (RouteCallback((ushort)info.LocalInput, (ushort)info.LocalOutput) != 0);
		}

		private IEnumerable<ConnectorInfo> SwitcherControlGetOutputsCallback()
		{
			int outputCount = GetOutputCountCallback == null ? 0 : GetOutputCountCallback();
			for (int index = 0; index < outputCount; index++)
				yield return new ConnectorInfo(index + 1, eConnectionType.Video | eConnectionType.Audio);
		}

		private IEnumerable<ConnectorInfo> SwitcherControlGetInputsCallback()
		{
			int inputCount = GetInputCountCallback == null ? 0 : GetInputCountCallback();
			for (int index = 0; index < inputCount; index++)
				yield return new ConnectorInfo(index + 1, eConnectionType.Video | eConnectionType.Audio);
		}

		private bool SwitcherControlGetSignalDetectedStateCallback(int input, eConnectionType type)
		{
			if (EnumUtils.HasMultipleFlags(type))
			{
				return EnumUtils.GetFlagsExceptNone(type)
				                .SelectMulti(f => SwitcherControlGetSignalDetectedStateCallback(input, f))
				                .Unanimous(false);
			}

			switch (type)
			{
				case eConnectionType.Audio:
				case eConnectionType.Video:
					return GetSignalDetectedStateCallback != null && (GetSignalDetectedStateCallback((ushort)input) != 0);
			}

			return false;
		}

		#endregion
	}
}

#endif
