using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LenovoLaptopBacklight.Models;

public class AppConfig
{
    [JsonPropertyName("register")]
    public int Register { get; set; } = -1;

    [JsonPropertyName("mirrorRegister")]
    public int MirrorRegister { get; set; } = -1;

    [JsonPropertyName("stages")]
    public List<Stage> Stages { get; set; } = new()
    {
        new Stage("Auto",   0x33),
        new Stage("Bright", 0x23),
        new Stage("Dim",    0x13),
        new Stage("Off",    0x03),
    };

    [JsonPropertyName("bootStage")]
    public string BootStage { get; set; } = "Off";

    [JsonPropertyName("bootEnabled")]
    public bool BootEnabled { get; set; } = false;

    [JsonPropertyName("triggerStartup")]
    public bool TriggerStartup { get; set; } = true;

    [JsonPropertyName("triggerLogon")]
    public bool TriggerLogon { get; set; } = true;

    [JsonPropertyName("triggerResume")]
    public bool TriggerResume { get; set; } = true;

    [JsonPropertyName("startupDelaySeconds")]
    public int StartupDelaySeconds { get; set; } = 25;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";

    [JsonPropertyName("logonDelaySeconds")]
    public int LogonDelaySeconds { get; set; } = 10;

    [JsonPropertyName("resumeDelaySeconds")]
    public int ResumeDelaySeconds { get; set; } = 8;

    public bool IsConfigured => Register >= 0;
}
