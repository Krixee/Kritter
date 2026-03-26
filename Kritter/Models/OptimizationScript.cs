using System.Text.Json.Serialization;

namespace Kritter.Models;

public enum ScriptFileType
{
    PowerShell,
    Cmd,
    Url,
    Lnk
}

public class OptimizationScript : ViewModels.BaseViewModel
{
    private bool _isSelected;

    [JsonPropertyName("relativePath")]
    public string RelativePath { get; set; } = "";

    [JsonPropertyName("name")]
    public string DisplayName { get; set; } = "";

    [JsonIgnore]
    public string FullPath { get; set; } = "";

    [JsonIgnore]
    public string Category { get; set; } = "";

    [JsonIgnore]
    public int SortOrder { get; set; }

    [JsonIgnore]
    public bool Is00Item { get; set; }

    [JsonIgnore]
    public ScriptFileType FileType { get; set; }

    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }
}
