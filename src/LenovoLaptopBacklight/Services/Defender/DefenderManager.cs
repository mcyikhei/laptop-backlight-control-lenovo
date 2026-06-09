using System;
using System.Diagnostics;
using System.IO;
using LenovoLaptopBacklight.Localization;

namespace LenovoLaptopBacklight.Services.Defender;

/// <summary>
/// Manages a Windows Defender exclusion for the bundled WinRing0 driver.
///
/// Defender flags WinRing0x64.sys as VulnerableDriver:WinNT/Winring0 and blocks the driver
/// from loading (StartService error 225 = ERROR_VIRUS_INFECTED). An AV path-exclusion on the
/// driver file lets it load. The app runs elevated, so it can call the Defender cmdlets.
/// </summary>
public static class DefenderManager
{
    public static string DriverPath =>
        Path.Combine(AppContext.BaseDirectory, "native", "WinRing0x64.sys");

    private static string AppDir => AppContext.BaseDirectory.TrimEnd('\\');

    public static bool HasExclusion()
    {
        var (code, stdout, _) = RunPs(
            "$p = (Get-MpPreference).ExclusionPath; " +
            $"if ($p -and ($p -contains '{DriverPath}')) {{ 'YES' }} else {{ 'NO' }}");
        return code == 0 && stdout.Trim().EndsWith("YES", StringComparison.OrdinalIgnoreCase);
    }

    public static (bool ok, string message) AddExclusion()
    {
        var (code, _, stderr) = RunPs(
            $"Add-MpPreference -ExclusionPath '{DriverPath}','{AppDir}'; " +
            $"Add-MpPreference -ExclusionProcess 'LaptopBacklightControl.exe'");
        return code == 0
            ? (true, Loc.I["def.added"])
            : (false, string.Format(Loc.I["def.addFailFmt"], Short(stderr)));
    }

    public static (bool ok, string message) RemoveExclusion()
    {
        var (code, _, stderr) = RunPs(
            $"Remove-MpPreference -ExclusionPath '{DriverPath}','{AppDir}'; " +
            $"Remove-MpPreference -ExclusionProcess 'LaptopBacklightControl.exe'");
        return code == 0
            ? (true, Loc.I["def.removed"])
            : (false, string.Format(Loc.I["def.removeFailFmt"], Short(stderr)));
    }

    private static string Short(string s) =>
        string.IsNullOrWhiteSpace(s) ? "(no detail)" : s.Trim().Split('\n')[0].Trim();

    private static (int code, string stdout, string stderr) RunPs(string command)
    {
        try
        {
            var psi = new ProcessStartInfo("powershell.exe",
                $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{command}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi)!;
            string outp = p.StandardOutput.ReadToEnd();
            string err = p.StandardError.ReadToEnd();
            p.WaitForExit(15000);
            return (p.ExitCode, outp, err);
        }
        catch (Exception ex)
        {
            return (-1, "", ex.Message);
        }
    }
}
