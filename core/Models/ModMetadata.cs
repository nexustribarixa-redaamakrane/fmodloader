using System.Collections.Generic;

namespace fModLoader.Models;

public class ModMetadata
{
    public string Name { get; set; } = "Unknown Mod";
    public string Version { get; set; } = "1.0";
    public string Author { get; set; } = "Unknown";
    public string Description { get; set; } = "";
    public string TargetFamily { get; set; } = "";
    public Dictionary<string, string> EmBox { get; set; } = new();
    public Dictionary<string, string> GlifMap { get; set; } = new();
    public string FilePath { get; set; } = "";

    public string DisplayName => $"{Name} v{Version} by {Author}";
}
