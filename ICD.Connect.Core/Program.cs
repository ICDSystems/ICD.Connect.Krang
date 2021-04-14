#if !SIMPLSHARP
using System;
using System.Runtime.InteropServices;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Krang.Cores;
using Microsoft.Win32;
using Topshelf;
using Topshelf.StartParameters;

namespace ICD.Connect.Core
{
	internal sealed class Options
	{
		public uint Program { get; set; } = 1;
	}

	internal static class Program
	{
		private const string SERVICE_NAME = "ICD.Connect.Core";

		private static IntPtr s_DeviceNotifyHandle;
		private static IntPtr s_DeviceEventHandle;
		private static Win32.ServiceControlHandlerEx s_ServiceControlCallback;

		/// <summary>
		/// Run as service.
		/// </summary>
		public static void Main()
		{
			Options options = new Options();

			TopshelfExitCode rc = HostFactory.Run(x =>
			{
				x.EnableStartParameters();
				x.EnableSessionChanged();

				x.WithStartParameter("program", p =>
				{
					uint program;
					options.Program = uint.TryParse(p, out program) ? program : 1;
				});

				x.Service<KrangBootstrap>(s =>
				{
					s.ConstructUsing(n => Construct(options));
					s.WhenStarted(Start);
					s.WhenStopped(Stop);
					s.WhenSessionChanged(HandleSessionChange);
				});

				x.RunAsLocalSystem();

				x.SetDisplayName("ICD.Connect.Core");
				x.SetServiceName(SERVICE_NAME);
				x.SetDescription("ICD Systems Core Application");

				x.SetStartTimeout(TimeSpan.FromMinutes(10));
				x.SetStopTimeout(TimeSpan.FromMinutes(10));

				x.StartAutomatically();

				x.BeforeInstall(() => BeforeInstall(options));
			});

			Environment.ExitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
		}

		private static KrangBootstrap Construct(Options options)
		{
			ProgramUtils.ProgramNumber = options.Program;

			bool isInteractive = IsConsoleApp();
			return new KrangBootstrap(isInteractive);
		}

		private static void Start(KrangBootstrap service)
		{
			IcdEnvironment.SetProgramStatus(IcdEnvironment.eProgramStatusEventType.Resumed);
			service.Start(null);
			RegisterDeviceNotification();
			IcdEnvironment.SetProgramInitializationComplete();
		}

		private static void Stop(KrangBootstrap service)
		{
			IcdEnvironment.SetProgramStatus(IcdEnvironment.eProgramStatusEventType.Stopping);
			service.Stop();
			UnregisterHandles();
		}

		private static void HandleSessionChange(KrangBootstrap service, HostControl host, SessionChangedArguments args)
		{
			IcdEnvironment.HandleSessionChange(args.SessionId, (IcdEnvironment.eSessionChangeEventType)args.ReasonCode);
		}

		private static void BeforeInstall(Options options)
		{
			// Create registry key for event logging
			string key = string.Format(@"SYSTEM\CurrentControlSet\Services\EventLog\Application\ICD.Connect.Core-{0}", 1);
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(key, true) ?? Registry.LocalMachine.CreateSubKey(key);
			if (registryKey == null)
				throw new ApplicationException("Failed to create registry key");

			// Setup the event log message file
			// Hack - Just using the regular .Net Runtime messages
			const string eventMessageFilePath = @"C:\Windows\System32\mscoree.dll";
			if (registryKey.GetValue("EventMessageFile") == null)
				registryKey.SetValue("EventMessageFile", eventMessageFilePath, RegistryValueKind.String);

			// Also copied from .Net Runtime
			if (registryKey.GetValue("TypesSupported") == null)
				registryKey.SetValue("TypesSupported", 0x07, RegistryValueKind.DWord);
		}

		private static bool IsConsoleApp()
		{
			try
			{
				// Hack
				int unused = Console.WindowHeight;
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Register for device notification from Windows
		/// </summary>
		private static void RegisterDeviceNotification()
		{
			if (IsConsoleApp())
				return;

			ILoggerService logger = ServiceProvider.GetService<ILoggerService>();

			s_ServiceControlCallback = ServiceControlHandler;

			IntPtr serviceHandle = Win32.RegisterServiceCtrlHandlerEx(SERVICE_NAME, s_ServiceControlCallback, IntPtr.Zero);

			if (serviceHandle == IntPtr.Zero)
			{
				logger.AddEntry(eSeverity.Error, "Service Handler Zero");
				return;
			}

			Win32.DevBroadcastDeviceInterface deviceInterface = new Win32.DevBroadcastDeviceInterface();
			int size = Marshal.SizeOf(deviceInterface);
			deviceInterface.dbcc_size = size;
			deviceInterface.dbcc_devicetype = Win32.DBT_DEVICE_TYPE_DEVICE_INTERFACE;
			
			IntPtr buffer;
			buffer = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(deviceInterface, buffer, true);
			
			s_DeviceEventHandle = Win32.RegisterDeviceNotification(serviceHandle, buffer, Win32.DEVICE_NOTIFY_SERVICE_HANDLE | Win32.DEVICE_NOTIFY_ALL_INTERFACE_CLASSES);
			if (s_DeviceEventHandle == IntPtr.Zero)
				logger.AddEntry(eSeverity.Error, "DeviceEvent Handle Zero - Device Connect/Disconnect Events Not Available");
		}

		/// <summary>
		/// Unregister for device notifications from Windows
		/// </summary>
		private static void UnregisterHandles()
		{
			if (s_DeviceNotifyHandle != IntPtr.Zero)
			{
				Win32.UnregisterDeviceNotification(s_DeviceNotifyHandle);
				s_DeviceNotifyHandle = IntPtr.Zero;
			}
		}

		/// <summary>
		/// Callback from Windows service for device notifications
		/// </summary>
		/// <param name="control"></param>
		/// <param name="eventType"></param>
		/// <param name="eventData"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		private static int ServiceControlHandler(int control, int eventType, IntPtr eventData, IntPtr context)
		{
			if (control == Win32.SERVICE_CONTROL_STOP || control == Win32.SERVICE_CONTROL_SHUTDOWN)
			{
				UnregisterHandles();
				Win32.UnregisterDeviceNotification(s_DeviceEventHandle);
			}
			else if (control == Win32.SERVICE_CONTROL_DEVICE_EVENT)
			{
				switch (eventType)
				{
					case Win32.DBT_DEVICE_ARRIVAL:
						IcdEnvironment.RaiseSystemDeviceAddedEvent();
						break;
					case Win32.DBT_DEVICE_REMOVE_COMPLETE:
						IcdEnvironment.RaiseSystemDeviceRemovedEvent();
						break;
				}
			}

			return 0;
		}
	}

	public static class Win32
	{
		public const int DEVICE_NOTIFY_SERVICE_HANDLE = 1;
		public const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;

		public const int SERVICE_CONTROL_STOP = 1;
		public const int SERVICE_CONTROL_DEVICE_EVENT = 11;
		public const int SERVICE_CONTROL_SHUTDOWN = 5;

		public const int DBT_DEVICE_TYPE_DEVICE_INTERFACE = 5;

		public const int DBT_DEVICE_ARRIVAL = 0x8000;
		public const int DBT_DEVICE_REMOVE_COMPLETE = 0x8004;

		public delegate int ServiceControlHandlerEx(int control, int eventType, IntPtr eventData, IntPtr context);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern IntPtr RegisterServiceCtrlHandlerEx(string lpServiceName, ServiceControlHandlerEx callbackEx, IntPtr context);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr RegisterDeviceNotification(IntPtr intPtr, IntPtr notificationFilter, Int32 flags);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern uint UnregisterDeviceNotification(IntPtr hHandle);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct DevBroadcastDeviceInterface
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
