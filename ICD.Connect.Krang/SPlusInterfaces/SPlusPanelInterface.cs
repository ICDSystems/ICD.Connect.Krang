#if SIMPLSHARP
using System;
using Crestron.SimplSharp;
using ICD.Common.Properties;
using ICD.Connect.Panels;
using ICD.Connect.Panels.EventArguments;
using ICD.Connect.Protocol.Sigs;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.SPlusInterfaces
{
	public sealed class SPlusPanelInterface : IDisposable
	{
		#region S+ Callbacks

		public delegate void DelJoinXsig(ushort smartObject, SimplSharpString xsig);

		[PublicAPI("SPlus")]
		public DelJoinXsig DigitalSigReceivedXsigCallback { get; set; }

		[PublicAPI("SPlus")]
		public DelJoinXsig AnalogSigReceivedXsigCallback { get; set; }

		[PublicAPI("SPlus")]
		public DelJoinXsig SerialSigReceivedXsigCallback { get; set; }

		#endregion

		/// <summary>
		/// Raised when settings are applied to the panel.
		/// </summary>
		[PublicAPI("SPlus")]
		public event EventHandler OnSettingsApplied;

		private ushort m_PanelId;
		private IPanelDevice m_PanelDevice;

		/// <summary>
		/// Constructor.
		/// </summary>
		public SPlusPanelInterface()
		{
			SPlusKrangBootstrap.OnKrangLoaded += SPlusKrangBootstrapOnKrangLoaded;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnSettingsApplied = null;
			SPlusKrangBootstrap.OnKrangLoaded -= SPlusKrangBootstrapOnKrangLoaded;

			SetPanel(0);
		}

		/// <summary>
		/// Called whent the krang core finishes loading.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SPlusKrangBootstrapOnKrangLoaded(object sender, EventArgs e)
		{
			SetPanel(m_PanelId);
		}

		/// <summary>
		/// Sets the wrapped panel.
		/// </summary>
		/// <param name="id"></param>
		[PublicAPI("SPlus")]
		public void SetPanel(ushort id)
		{
			m_PanelId = id;

			Unsubscribe(m_PanelDevice);
			m_PanelDevice = GetPanelDevice(id);
			Subscribe(m_PanelDevice);
		}

		/// <summary>
		/// Sends the serial data to the panel.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="text"></param>
		[PublicAPI("S+")]
		public void SendInputSerial(ushort number, SimplSharpString text)
		{
			if (m_PanelDevice == null)
				return;

			m_PanelDevice.SendInputSerial(number, text.ToString());
		}

		/// <summary>
		/// Sends the analog data to the panel.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="value"></param>
		[PublicAPI("S+")]
		public void SendInputAnalog(ushort number, ushort value)
		{
			if (m_PanelDevice == null)
				return;

			m_PanelDevice.SendInputAnalog(number, value);
		}

		/// <summary>
		/// Sends the digital data to the panel.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="value"></param>
		[PublicAPI("S+")]
		public void SendInputDigital(ushort number, ushort value)
		{
			if (m_PanelDevice == null)
				return;

			m_PanelDevice.SendInputDigital(number, value != 0);
		}

		#region Panel Callbacks

		/// <summary>
		/// Subscribe to the panel events.
		/// </summary>
		/// <param name="panelDevice"></param>
		private void Subscribe(IPanelDevice panelDevice)
		{
			if (panelDevice == null)
				return;

			panelDevice.OnAnyOutput += PanelDeviceOnAnyOutput;
		}

		/// <summary>
		/// Unsubscribe from the panel events.
		/// </summary>
		/// <param name="panelDevice"></param>
		private void Unsubscribe(IPanelDevice panelDevice)
		{
			if (panelDevice == null)
				return;

			panelDevice.OnAnyOutput -= PanelDeviceOnAnyOutput;
		}

		/// <summary>
		/// Called when a panel output sig changes state.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void PanelDeviceOnAnyOutput(object sender, SigInfoEventArgs eventArgs)
		{
			SigInfo sig = eventArgs.Data;
			DelJoinXsig handler;

			switch (sig.Type)
			{
				case eSigType.Digital:
					handler = DigitalSigReceivedXsigCallback;
					break;

				case eSigType.Analog:
					handler = AnalogSigReceivedXsigCallback;
					break;

				case eSigType.Serial:
					handler = SerialSigReceivedXsigCallback;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			if (handler != null)
				handler(sig.SmartObject, sig.ToXSig());
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the panel device with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[CanBeNull]
		private IPanelDevice GetPanelDevice(ushort id)
		{
			IOriginator device;
			if (!SPlusKrangBootstrap.Krang.Originators.TryGetChild(id, out device))
				return null;

			return device as IPanelDevice;
		}

		#endregion
	}
}
#endif
