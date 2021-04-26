#if !SIMPLSHARP
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Core
{
	public static class Win32
	{
		private const int ERROR_NO_ERROR = 0;
		private const int ERROR_CALL_NOT_IMPLEMENTED = 0x00000078;

		private const int DEVICE_NOTIFY_SERVICE_HANDLE = 1;
		private const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;

		private const int SERVICE_CONTROL_STOP = 1;
		private const int SERVICE_CONTROL_INTERROGATE = 4;
		private const int SERVICE_CONTROL_SHUTDOWN = 5;
		private const int SERVICE_CONTROL_DEVICE_EVENT = 11;

		private const int DBT_DEVICE_TYPE_DEVICE_INTERFACE = 5;
		private const int DBT_DEVICE_ARRIVAL = 0x8000;
		private const int DBT_DEVICE_REMOVE_COMPLETE = 0x8004;

		/// <summary>
		/// Raised when a device is connected.
		/// </summary>
		public static event EventHandler OnSystemDeviceAdded;

		/// <summary>
		/// Raised when a device is disconnected.
		/// </summary>
		public static event EventHandler OnSystemDeviceRemoved;

		/// <summary>
		/// Raised when the service is signaled to stop or shutdown.
		/// </summary>
		public static event EventHandler OnServiceControlStopShutdown;

		private delegate int ServiceControlHandlerEx(int control, int eventType, IntPtr eventData, IntPtr context);

		private static readonly ServiceControlHandlerEx s_ServiceControlCallback;

		private static IntPtr s_DeviceNotifyHandle;
		private static IntPtr s_DeviceEventHandle;

		/// <summary>
		/// Static constructor.
		/// </summary>
		static Win32()
		{
			s_ServiceControlCallback = ServiceControlHandler;
		}

		#region Service Control Handler

		/// <summary>
		/// Register for device notification from Windows.
		/// </summary>
		public static void RegisterDeviceNotifications(string serviceName)
		{
			UnregisterDeviceNotifications();

			IntPtr serviceHandle = RegisterServiceCtrlHandlerEx(serviceName, s_ServiceControlCallback, IntPtr.Zero);
			if (serviceHandle == IntPtr.Zero)
				throw new InvalidOperationException("Failed to register a service handler");

			DevBroadcastDeviceInterface deviceInterface = new DevBroadcastDeviceInterface();
			int size = Marshal.SizeOf(deviceInterface);
			deviceInterface.dbcc_size = size;
			deviceInterface.dbcc_devicetype = DBT_DEVICE_TYPE_DEVICE_INTERFACE;

			IntPtr buffer = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(deviceInterface, buffer, true);

			s_DeviceEventHandle = RegisterDeviceNotification(serviceHandle, buffer, DEVICE_NOTIFY_SERVICE_HANDLE | DEVICE_NOTIFY_ALL_INTERFACE_CLASSES);
			if (s_DeviceEventHandle == IntPtr.Zero)
				throw new InvalidOperationException("DeviceEvent Handle Zero - Device Connect/Disconnect Events Not Available");
		}

		/// <summary>
		/// Unregister for device notifications from Windows.
		/// </summary>
		public static void UnregisterDeviceNotifications()
		{
			if (s_DeviceNotifyHandle == IntPtr.Zero)
				return;

			UnregisterDeviceNotification(s_DeviceNotifyHandle);
			s_DeviceNotifyHandle = IntPtr.Zero;
		}

		/// <summary>
		/// Callback from Windows service for device notifications.
		/// </summary>
		/// <param name="control"></param>
		/// <param name="eventType"></param>
		/// <param name="eventData"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		private static int ServiceControlHandler(int control, int eventType, IntPtr eventData, IntPtr context)
		{
			switch (control)
			{
				case SERVICE_CONTROL_INTERROGATE:
					// From the RegisterServiceCtrlHandlerEx documentation
					return ERROR_NO_ERROR;

				case SERVICE_CONTROL_STOP:
				case SERVICE_CONTROL_SHUTDOWN:
					UnregisterDeviceNotifications();
					OnServiceControlStopShutdown.Raise(null);
					return ERROR_NO_ERROR;

				case SERVICE_CONTROL_DEVICE_EVENT:
					switch (eventType)
					{
						case DBT_DEVICE_ARRIVAL:
							OnSystemDeviceAdded.Raise(null);
							break;
						case DBT_DEVICE_REMOVE_COMPLETE:
							OnSystemDeviceRemoved.Raise(null);
							break;
					}
					return ERROR_NO_ERROR;
			}

			// From the RegisterServiceCtrlHandlerEx documentation
			return ERROR_CALL_NOT_IMPLEMENTED;
		}

		#endregion

		#region Private Methods

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern IntPtr RegisterServiceCtrlHandlerEx(string lpServiceName, ServiceControlHandlerEx callbackEx, IntPtr context);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr RegisterDeviceNotification(IntPtr intPtr, IntPtr notificationFilter, Int32 flags);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern uint UnregisterDeviceNotification(IntPtr hHandle);

		#endregion

		[SuppressMessage("ReSharper", "All")]
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct DevBroadcastDeviceInterface
		{
			public int dbcc_size;
			public int dbcc_devicetype;
			public int dbcc_reserved;
			[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
			public byte[] dbcc_classguid;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
			public char[] dbcc_name;
		}
	}
}
#endif
