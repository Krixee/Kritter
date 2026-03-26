using System.IO;
using System.Text.Json.Serialization;

namespace Kritter.Models;

public class SetupInstaller : ViewModels.BaseViewModel
{
    private bool _isSelected = true;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = "";

    [JsonPropertyName("packagePath")]
    public string PackagePath { get; set; } = "";

    [JsonPropertyName("silentArgs")]
    public string? SilentArgs { get; set; }

    [JsonIgnore]
    public string SourceFilePath { get; set; } = "";

    [JsonIgnore]
    public string ResolvedFilePath { get; set; } = "";

    [JsonIgnore]
    public string MethodLabel => "Setup";

    [JsonIgnore]
    public string EffectiveFilePath =>
        !string.IsNullOrWhiteSpace(ResolvedFilePath) ? ResolvedFilePath : SourceFilePath;

    [JsonIgnore]
    public string DisplayFileName =>
        !string.IsNullOrWhiteSpace(FileName)
            ? FileName
            : (!string.IsNullOrWhiteSpace(EffectiveFilePath) ? Path.GetFileName(EffectiveFilePath) : "");

    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public SetupInstaller CloneForPackage()
    {
        return new SetupInstaller
        {
            DisplayName = DisplayName,
            FileName = DisplayFileName,
            PackagePath = PackagePath,
            SilentArgs = SilentArgs,
            SourceFilePath = SourceFilePath,
            ResolvedFilePath = ResolvedFilePath,
            IsSelected = IsSelected
        };
    }
}
