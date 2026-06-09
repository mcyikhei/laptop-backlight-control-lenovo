using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace LenovoLaptopBacklight.Services.Ec;

/// <summary>
/// Reads and writes the Embedded Controller RAM via the WinRing0x64 kernel driver,
/// using direct DeviceIoControl calls (no WinRing0.dll user-mode wrapper required).
///
/// ACPI EC protocol (ACPI spec §12.9):
///   Data port:           0x62
///   Command/Status port: 0x66
///   READ command:  0x80   WRITE command: 0x81
///   Status bits: bit0 = OBF (data ready to read), bit1 = IBF (busy, don't write yet)
///
/// EC access is coordinated with the OS ACPI driver / other tools through the global
/// "Access_EC" mutex (the same one LibreHardwareMonitor / OpenHardwareMonitor use), plus
/// per-transaction retry so a collision with the OS does not surface as a hard failure.
/// </summary>
public sealed class EcController : IDisposable
{
    // WinRing0 IOCTL codes (OLS_TYPE=40000=0x9C40). The driver defines these with
    // FILE_READ_ACCESS (read) / FILE_WRITE_ACCESS (write) — those access bits are part
    // of the control code and MUST match, or DeviceIoControl returns ERROR_INVALID_FUNCTION (1).
    //   CTL_CODE(0x9C40, 0x833, METHOD_BUFFERED, FILE_READ_ACCESS)  = 0x9C4060CC
    //   CTL_CODE(0x9C40, 0x836, METHOD_BUFFERED, FILE_WRITE_ACCESS) = 0x9C40A0D8
    private const uint IOCTL_READ_IO_PORT_BYTE  = 0x9C4060CC; // func=0x833, read access
    private const uint IOCTL_WRITE_IO_PORT_BYTE = 0x9C40A0D8; // func=0x836, write access

    private const ushort EC_DATA    = 0x62;
    private const ushort EC_COMMAND = 0x66;
    private const byte   EC_READ    = 0x80;
    private const byte   EC_WRITE   = 0x81;

    private const int  STATUS_TIMEOUT_MS = 350;  // per-attempt wait for an IBF/OBF transition
    private const int  TRANSACTION_RETRIES = 6;  // whole-transaction retries on collision
    private const int  EC_MUTEX_WAIT_MS = 500;

    private readonly DriverService _driver;
    private readonly Mutex? _ecMutex;
    private bool _disposed;

    public EcController()
    {
        _driver = new DriverService();
        string sysPath = Path.Combine(AppContext.BaseDirectory, "native", "WinRing0x64.sys");
        _driver.EnsureDriverAndOpen(sysPath);
        _ecMutex = OpenGlobalEcMutex();
    }

    // --- Public API (each takes the EC lock) ---

    public byte ReadByte(int register) => WithEcLock(() => ReadByteRaw(register));

    public void WriteByte(int register, byte value) => WithEcLock(() => { WriteByteRaw(register, value); return 0; });

    /// <summary>Dump all 256 EC registers under a single EC lock for speed/consistency.</summary>
    public byte[] DumpAll() => WithEcLock(() =>
    {
        var result = new byte[256];
        for (int i = 0; i < 256; i++)
            result[i] = ReadByteRaw(i);
        return result;
    });

    // --- Transactions with retry/recovery (no locking; callers hold the EC lock) ---

    private byte ReadByteRaw(int register)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                WaitIbfClear();
                WritePort(EC_COMMAND, EC_READ);
                WaitIbfClear();
                WritePort(EC_DATA, (byte)register);
                WaitObfSet();
                return ReadPort(EC_DATA);
            }
            catch (TimeoutException) when (attempt < TRANSACTION_RETRIES)
            {
                RecoverFromCollision();
            }
        }
    }

    private void WriteByteRaw(int register, byte value)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                WaitIbfClear();
                WritePort(EC_COMMAND, EC_WRITE);
                WaitIbfClear();
                WritePort(EC_DATA, (byte)register);
                WaitIbfClear();
                WritePort(EC_DATA, value);
                return;
            }
            catch (TimeoutException) when (attempt < TRANSACTION_RETRIES)
            {
                RecoverFromCollision();
            }
        }
    }

    /// <summary>
    /// After a timed-out transaction the EC may have a stale byte waiting (OBF set) or be
    /// mid-command. Drain any pending output and pause briefly so the next attempt starts clean.
    /// </summary>
    private void RecoverFromCollision()
    {
        try
        {
            for (int i = 0; i < 4 && (ReadPort(EC_COMMAND) & 0x01) != 0; i++)
                ReadPort(EC_DATA);
        }
        catch { /* ignore — best-effort drain */ }
        Thread.Sleep(2);
    }

    // --- EC mutex coordination ---

    private static Mutex? OpenGlobalEcMutex()
    {
        foreach (var name in new[] { "Global\\Access_EC", "Global\\Access_PM2" })
        {
            try { return Mutex.OpenExisting(name); }
            catch (WaitHandleCannotBeOpenedException) { }
            catch (UnauthorizedAccessException) { }
            catch { }
        }
        return null; // no OS-provided EC mutex on this machine — proceed unsynchronized
    }

    private T WithEcLock<T>(Func<T> action)
    {
        bool held = false;
        try
        {
            if (_ecMutex != null)
            {
                try { held = _ecMutex.WaitOne(EC_MUTEX_WAIT_MS); }
                catch (AbandonedMutexException) { held = true; } // previous owner crashed — we own it now
            }
            return action();
        }
        finally
        {
            if (held)
            {
                try { _ecMutex!.ReleaseMutex(); } catch { }
            }
        }
    }

    // --- ACPI EC status helpers ---

    private void WaitIbfClear()
    {
        var deadline = Environment.TickCount64 + STATUS_TIMEOUT_MS;
        while ((ReadPort(EC_COMMAND) & 0x02) != 0)
        {
            if (Environment.TickCount64 > deadline)
                throw new TimeoutException("EC IBF never cleared");
            Thread.SpinWait(50);
        }
    }

    private void WaitObfSet()
    {
        var deadline = Environment.TickCount64 + STATUS_TIMEOUT_MS;
        while ((ReadPort(EC_COMMAND) & 0x01) == 0)
        {
            if (Environment.TickCount64 > deadline)
                throw new TimeoutException("EC OBF never set");
            Thread.SpinWait(50);
        }
    }

    // --- WinRing0 port I/O via DeviceIoControl ---

    private unsafe byte ReadPort(ushort port)
    {
        uint inData = port;
        uint outData = 0;
        uint returned;
        if (!NativeMethods.DeviceIoControl(_driver.DeviceHandle, IOCTL_READ_IO_PORT_BYTE,
                &inData, sizeof(uint), &outData, sizeof(uint), out returned, nint.Zero))
            throw new InvalidOperationException($"IOCTL ReadPort 0x{port:X4} failed: {Marshal.GetLastWin32Error()}");
        return (byte)(outData & 0xFF);
    }

    private unsafe void WritePort(ushort port, byte value)
    {
        // WinRing0 OLS_WRITE_IO_PORT_INPUT: ULONG PortNumber (offset 0) + union(ULONG/USHORT/UCHAR)
        // at offset 4 -> 8 bytes total. Encode port in low DWORD, value in the byte at offset 4.
        ulong inData = (uint)port | ((ulong)value << 32);
        uint returned;
        if (!NativeMethods.DeviceIoControl(_driver.DeviceHandle, IOCTL_WRITE_IO_PORT_BYTE,
                &inData, sizeof(ulong), null, 0, out returned, nint.Zero))
            throw new InvalidOperationException($"IOCTL WritePort 0x{port:X4}=0x{value:X2} failed: {Marshal.GetLastWin32Error()}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _driver.Dispose();
        _ecMutex?.Dispose();
    }
}
