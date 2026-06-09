using System.Text.Json.Serialization;

namespace LenovoLaptopBacklight.Models;

public class Stage
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("value")]
    public byte Value { get; set; }

    public Stage() { }
    public Stage(string name, byte value) { Name = name; Value = value; }

    public override string ToString() => $"{Name} (0x{Value:X2})";
}
