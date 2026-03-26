using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Kritter.Models;

public enum OptimizationMode
{
    KritterRecommended,
    Fr33tyAll,
    KeepCurrent
}

public class KritterPackage
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("optimizationMode")]
    public OptimizationMode OptimizationMode { get; set; }

    [JsonPropertyName("apps")]
    public List<WingetApp> Apps { get; set; } = new();

    [JsonPropertyName("fr33tyScripts")]
    public List<OptimizationScript> Fr33tyScripts { get; set; } = new();

    [JsonPropertyName("setupInstallers")]
    public List<SetupInstaller> SetupInstallers { get; set; } = new();
}

public class ResumeInfo
{
    [JsonPropertyName("packagePath")]
    public string PackagePath { get; set; } = "";

    [JsonPropertyName("phase")]
    public string Phase { get; set; } = "";

    [JsonPropertyName("logHistory")]
    public List<string> LogHistory { get; set; } = new();
}
