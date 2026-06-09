using System;
using System.Runtime.InteropServices;

namespace LenovoLaptopBacklight.Services.Ec;

internal static class NativeMethods
{
    internal const uint GENERIC_READ        = 0x80000000;
    internal const uint GENERIC_WRITE       = 0x40000000;
    internal const uint OPEN_EXISTING       = 3;
    internal const uint FILE_ATTRIBUTE_NORMAL = 0x80;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern nint CreateFile(string lpFileName, uint dwDesiredAccess,
        uint dwShareMode, nint lpSecurityAttributes, uint dwCreationDisposition,
        uint dwFlagsAndAttributes, nint hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(nint hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern unsafe bool DeviceIoControl(nint hDevice, uint dwIoControlCode,
        void* lpInBuffer, uint nInBufferSize, void* lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, nint lpOverlapped);
}
