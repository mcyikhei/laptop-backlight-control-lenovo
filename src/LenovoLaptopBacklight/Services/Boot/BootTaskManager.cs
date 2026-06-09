using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using LenovoLaptopBacklight.Models;

namespace LenovoLaptopBacklight.Services.Boot;

public static class BootTaskManager
{
    private const string TaskName = "LaptopBacklightControl-Apply";

    public static bool TaskExists()
    {
        var r = Schtasks("/Query", $"/TN \"{TaskName}\"");
        return r.ExitCode == 0;
    }

    public static void Register(AppConfig cfg)
    {
        string exePath = Path.Combine(AppContext.BaseDirectory, "LaptopBacklightControl.exe");
        string xmlPath = Path.Combine(Path.GetTempPath(), "LaptopBacklightControl-task.xml");
        File.WriteAllText(xmlPath, BuildXml(exePath, cfg), Encoding.Unicode);

        // Delete first (ignore error if not present), then create from XML
        Schtasks("/Delete", $"/TN \"{TaskName}\" /F");
        var r = Schtasks("/Create", $"/XML \"{xmlPath}\" /TN \"{TaskName}\"");
        File.Delete(xmlPath);
        if (r.ExitCode != 0)
            throw new InvalidOperationException(string.Format(
                Localization.Loc.I["boot.createFailFmt"], $"exit {r.ExitCode}\n{r.Stderr}"));
    }

    public static void Unregister()
        => Schtasks("/Delete", $"/TN \"{TaskName}\" /F");

    public static void RunNow()
    {
        var r = Schtasks("/Run", $"/TN \"{TaskName}\"");
        if (r.ExitCode != 0)
            throw new InvalidOperationException(string.Format(Localization.Loc.I["boot.runFailFmt"], r.Stderr));
    }

    // --- XML builder ---

    private static string Iso(int seconds) =>
        seconds >= 60 ? $"PT{seconds / 60}M{seconds % 60}S" : $"PT{seconds}S";

    private static string BuildXml(string exePath, AppConfig cfg)
    {
        // Run as the current interactive user, elevated (HighestAvailable) — InteractiveToken means
        // no stored password and no UAC prompt. Triggers fire inside the user's session at logon
        // (and optionally on resume), which is far more reliable than a SYSTEM task at early boot
        // (where Defender blocked the WinRing0 driver load).
        string user = WindowsIdentity.GetCurrent().Name; // e.g. "MACHINE\\User"

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-16\"?>");
        sb.AppendLine("<Task version=\"1.4\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">");
        sb.AppendLine("  <RegistrationInfo>");
        sb.AppendLine("    <Description>Laptop Backlight Control for Lenovo - apply backlight stage at logon</Description>");
        sb.AppendLine("  </RegistrationInfo>");
        sb.AppendLine("  <Principals>");
        sb.AppendLine("    <Principal id=\"Author\">");
        sb.AppendLine($"      <UserId>{SecurityEscape(user)}</UserId>");
        sb.AppendLine("      <LogonType>InteractiveToken</LogonType>");
        sb.AppendLine("      <RunLevel>HighestAvailable</RunLevel>");
        sb.AppendLine("    </Principal>");
        sb.AppendLine("  </Principals>");
        sb.AppendLine("  <Settings>");
        sb.AppendLine("    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>");
        sb.AppendLine("    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>");
        sb.AppendLine("    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>");
        sb.AppendLine("    <AllowHardTerminate>true</AllowHardTerminate>");
        sb.AppendLine("    <StartWhenAvailable>true</StartWhenAvailable>");
        sb.AppendLine("    <ExecutionTimeLimit>PT2M</ExecutionTimeLimit>");
        sb.AppendLine("    <Enabled>true</Enabled>");
        sb.AppendLine("  </Settings>");
        sb.AppendLine("  <Triggers>");
        // Logon trigger scoped to this user (always present)
        sb.AppendLine("    <LogonTrigger>");
        sb.AppendLine("      <Delay>" + Iso(cfg.LogonDelaySeconds) + "</Delay>");
        sb.AppendLine($"      <UserId>{SecurityEscape(user)}</UserId>");
        sb.AppendLine("    </LogonTrigger>");
        if (cfg.TriggerResume)
        {
            sb.AppendLine("    <EventTrigger>");
            sb.AppendLine("      <Delay>" + Iso(cfg.ResumeDelaySeconds) + "</Delay>");
            sb.AppendLine("      <Subscription>&lt;QueryList&gt;&lt;Query Id=\"0\" Path=\"System\"&gt;&lt;Select Path=\"System\"&gt;*[System[Provider[@Name='Microsoft-Windows-Power-Troubleshooter'] and (EventID=1)]]&lt;/Select&gt;&lt;/Query&gt;&lt;/QueryList&gt;</Subscription>");
            sb.AppendLine("    </EventTrigger>");
        }
        sb.AppendLine("  </Triggers>");
        sb.AppendLine("  <Actions Context=\"Author\">");
        sb.AppendLine($"    <Exec><Command>{SecurityEscape(exePath)}</Command><Arguments>--apply</Arguments></Exec>");
        sb.AppendLine("  </Actions>");
        sb.AppendLine("</Task>");
        return sb.ToString();
    }

    private static string SecurityEscape(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static (int ExitCode, string Stdout, string Stderr) Schtasks(string verb, string args)
    {
        var psi = new ProcessStartInfo("schtasks.exe", $"{verb} {args}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };
        using var p = Process.Start(psi)!;
        string stdout = p.StandardOutput.ReadToEnd();
        string stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, stdout, stderr);
    }
}
