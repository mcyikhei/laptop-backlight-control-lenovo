using System;
using System.IO;
using System.Text.Json;
using LenovoLaptopBacklight.Models;

namespace LenovoLaptopBacklight.Services.Config;

public static class ConfigStore
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     "LenovoLaptopBacklight");

    public static string ConfigPath => Path.Combine(Dir, "config.json");
    public static string LogPath    => Path.Combine(Dir, "backlight.log");

    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json, Opts) ?? new AppConfig();
            }
        }
        catch { /* return defaults on any parse error */ }
        return new AppConfig();
    }

    public static void Save(AppConfig cfg)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(cfg, Opts));
    }

    public static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.AppendAllText(LogPath,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
        }
        catch { /* best-effort logging */ }
    }
}
