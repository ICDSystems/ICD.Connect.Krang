using System;
using System.Collections.Generic;
using ICD.Connect.API.EventArguments;
using ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.Proxy;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.SPlusTouchpanel.EventArgs
{
	public sealed class SetAudioSourceIndexApiEventArgs : AbstractGenericApiEventArgs<int>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SetAudioSourceIndexApiEventArgs(int data) : base(SPlusTouchpanelDeviceApi.EVENT_SET_AUDIO_SOURCE_INDEX, data)
		{
		}
	}
}