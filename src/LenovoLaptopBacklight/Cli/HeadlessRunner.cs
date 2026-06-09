using System;
using System.Linq;
using LenovoLaptopBacklight.Services.Config;
using LenovoLaptopBacklight.Services.Ec;

namespace LenovoLaptopBacklight.Cli;

/// <summary>
/// Headless CLI mode — used by the scheduled boot task.
/// Usage:
///   --apply               Write the configured bootStage and exit
///   --set &lt;stage-name&gt;   Write a named stage and exit
///   --read                Print the current register value and exit
/// </summary>
public static class HeadlessRunner
{
    public static int Run(string[] args)
    {
        var cfg = ConfigStore.Load();

        if (!cfg.IsConfigured)
        {
            ConfigStore.Log("HEADLESS: no register configured — nothing to do");
            return 1;
        }

        string cmd = args[0].ToLowerInvariant();

        try
        {
            using var ec = new EcController();

            if (cmd == "--read")
            {
                byte val = ec.ReadByte(cfg.Register);
                Console.WriteLine($"0x{cfg.Register:X2} = 0x{val:X2} ({val})");
                return 0;
            }

            string stageName = cmd == "--apply"
                ? cfg.BootStage
                : (args.Length >= 2 ? args[1] : "");

            var stage = cfg.Stages.FirstOrDefault(
                s => s.Name.Equals(stageName, StringComparison.OrdinalIgnoreCase));

            if (stage == null)
            {
                ConfigStore.Log($"HEADLESS: stage '{stageName}' not found in config");
                return 2;
            }

            // Primary register only. Writing the mirror register back-to-back suppresses the
            // actual LED change (same fix as the Control panel and the Wizard's Verify step).
            ec.WriteByte(cfg.Register, stage.Value);

            // Readback verify
            byte rb = ec.ReadByte(cfg.Register);
            bool ok = rb == stage.Value;
            ConfigStore.Log(ok
                ? $"HEADLESS {cmd}: wrote '{stage.Name}' (0x{stage.Value:X2}) to 0x{cfg.Register:X2} — OK"
                : $"HEADLESS {cmd}: wrote '{stage.Name}' (0x{stage.Value:X2}) but readback=0x{rb:X2} — MISMATCH");

            return ok ? 0 : 3;
        }
        catch (Exception ex)
        {
            ConfigStore.Log($"HEADLESS {cmd}: ERROR — {ex.Message}");
            return 99;
        }
    }
}
