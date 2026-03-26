using System.Text.Json.Serialization;

namespace Kritter.Models;

public enum InstallMethod
{
    Winget,
    Direct,
    Setup
}

public class WingetApp : ViewModels.BaseViewModel
{
    private bool _isSelected;

    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("installMethod")]
    public InstallMethod InstallMethod { get; set; } = InstallMethod.Winget;

    [JsonPropertyName("directInstallerUrl")]
    public string? DirectInstallerUrl { get; set; }

    [JsonPropertyName("directInstallerArgs")]
    public string? DirectInstallerArgs { get; set; }

    [JsonIgnore]
    public bool IsInstalled { get; set; }

    [JsonIgnore]
    public string MethodLabel => InstallMethod switch
    {
        InstallMethod.Winget => "Winget",
        InstallMethod.Direct => "Direct",
        InstallMethod.Setup => "Setup",
        _ => "Unknown"
    };

    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }
}
