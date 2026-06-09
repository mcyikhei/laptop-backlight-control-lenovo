using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace LenovoLaptopBacklight.Services.Ec;

/// <summary>
/// Installs, starts, and (on dispose) optionally removes the WinRing0x64 kernel driver service.
/// We talk directly to the driver via DeviceIoControl so no WinRing0.dll is needed.
/// </summary>
public sealed class DriverService : IDisposable
{
    private const string ServiceName = "WinRing0_1_2_0";
    private const string DeviceName  = @"\\.\WinRing0_1_2_0";

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern nint OpenSCManager(string? machine, string? database, uint access);
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern nint CreateService(nint hSCM, string name, string displayName,
        uint desiredAccess, uint serviceType, uint startType, uint errorControl,
        string binaryPath, string? loadOrderGroup, nint tagId, string? dependencies,
        string? serviceStartName, string? password);
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern nint OpenService(nint hSCM, string name, uint desiredAccess);
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool StartService(nint hService, uint numArgs, nint argVectors);
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ControlService(nint hService, uint control, out ServiceStatus status);
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteService(nint hService);
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseServiceHandle(nint hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct ServiceStatus
    {
        public uint ServiceType, CurrentState, ControlsAccepted,
                    Win32ExitCode, ServiceSpecificExitCode, CheckPoint, WaitHint;
    }

    private const uint SC_MANAGER_ALL      = 0xF003F;
    private const uint SERVICE_ALL_ACCESS  = 0xF01FF;
    private const uint SERVICE_KERNEL_DRV  = 0x00000001;
    private const uint SERVICE_DEMAND_START= 0x00000003;
    private const uint SERVICE_ERROR_NORMAL= 0x00000001;
    private const uint SERVICE_CONTROL_STOP= 0x00000001;
    private const uint SERVICE_STOPPED     = 0x00000001;
    private const uint SERVICE_RUNNING     = 0x00000004;
    private const uint ERROR_SERVICE_EXISTS= 1073;
    private const uint ERROR_SERVICE_ALREADY_RUNNING = 1056;
    private const int  ERROR_VIRUS_INFECTED = 225; // AV (Defender) blocked the driver file

    private bool _weInstalledIt;
    private bool _disposed;

    private static readonly nint InvalidHandle = new(-1);
    public nint DeviceHandle { get; private set; } = new(-1);
    public bool IsOpen => DeviceHandle != InvalidHandle && DeviceHandle != 0;

    public void EnsureDriverAndOpen(string sysPath)
    {
        if (!File.Exists(sysPath))
            throw new FileNotFoundException($"WinRing0x64.sys not found at: {sysPath}");

        // Try opening the device first (driver already running from a prior run)
        DeviceHandle = NativeMethods.CreateFile(DeviceName,
            NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
            0, nint.Zero, NativeMethods.OPEN_EXISTING,
            NativeMethods.FILE_ATTRIBUTE_NORMAL, nint.Zero);

        if (IsOpen) return;

        // Install + start the driver
        nint hSCM = OpenSCManager(null, null, SC_MANAGER_ALL);
        if (hSCM == 0) throw new InvalidOperationException($"OpenSCManager failed: {Marshal.GetLastWin32Error()}");
        try
        {
            nint hSvc = CreateService(hSCM, ServiceName, ServiceName,
                SERVICE_ALL_ACCESS, SERVICE_KERNEL_DRV, SERVICE_DEMAND_START, SERVICE_ERROR_NORMAL,
                sysPath, null, nint.Zero, null, null, null);

            int err = Marshal.GetLastWin32Error();
            if (hSvc == 0 && err == ERROR_SERVICE_EXISTS)
                hSvc = OpenService(hSCM, ServiceName, SERVICE_ALL_ACCESS);

            if (hSvc == 0) throw new InvalidOperationException($"CreateService/OpenService failed: {err}");

            try
            {
                _weInstalledIt = (err != (int)ERROR_SERVICE_EXISTS);
                bool started = StartService(hSvc, 0, nint.Zero);
                int startErr = Marshal.GetLastWin32Error();
                if (!started && startErr != ERROR_SERVICE_ALREADY_RUNNING)
                {
                    if (startErr == ERROR_VIRUS_INFECTED)
                        throw new InvalidOperationException(Localization.Loc.I["drv.defenderBlocked"]);
                    throw new InvalidOperationException($"StartService failed: {startErr}");
                }
            }
            finally { CloseServiceHandle(hSvc); }
        }
        finally { CloseServiceHandle(hSCM); }

        // Retry opening the device after install
        Thread.Sleep(200);
        DeviceHandle = NativeMethods.CreateFile(DeviceName,
            NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
            0, nint.Zero, NativeMethods.OPEN_EXISTING,
            NativeMethods.FILE_ATTRIBUTE_NORMAL, nint.Zero);

        if (!IsOpen)
            throw new InvalidOperationException($"Failed to open {DeviceName} after driver start: {Marshal.GetLastWin32Error()}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (IsOpen) { NativeMethods.CloseHandle(DeviceHandle); DeviceHandle = new(-1); }

        if (_weInstalledIt) RemoveDriver();
    }

    private static void RemoveDriver()
    {
        nint hSCM = OpenSCManager(null, null, SC_MANAGER_ALL);
        if (hSCM == 0) return;
        try
        {
            nint hSvc = OpenService(hSCM, ServiceName, SERVICE_ALL_ACCESS);
            if (hSvc == 0) return;
            try
            {
                ControlService(hSvc, SERVICE_CONTROL_STOP, out _);
                Thread.Sleep(300);
                DeleteService(hSvc);
            }
            finally { CloseServiceHandle(hSvc); }
        }
        finally { CloseServiceHandle(hSCM); }
    }
}
