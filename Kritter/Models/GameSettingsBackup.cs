using System.IO;
using System.Text.Json.Serialization;
using Kritter.Localization;

namespace Kritter.Models;

public enum GameSettingsKind
{
    Cs2
}

public class GameSettingsBackup : ViewModels.BaseViewModel
{
    private bool _isSelected = true;

    [JsonPropertyName("kind")]
    public GameSettingsKind Kind { get; set; }

    [JsonPropertyName("gameName")]
    public string GameName { get; set; } = "";

    [JsonPropertyName("accountId")]
    public string AccountId { get; set; } = "";

    [JsonPropertyName("steamId64")]
    public string SteamId64 { get; set; } = "";

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = "";

    [JsonPropertyName("personaName")]
    public string PersonaName { get; set; } = "";

    [JsonPropertyName("avatarUrl")]
    public string AvatarUrl { get; set; } = "";

    [JsonPropertyName("packagePath")]
    public string PackagePath { get; set; } = "";

    [JsonIgnore]
    public string SourcePath { get; set; } = "";

    [JsonIgnore]
    public string ResolvedPath { get; set; } = "";

    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public string EffectiveSourcePath =>
        !string.IsNullOrWhiteSpace(ResolvedPath) ? ResolvedPath : SourcePath;

    [JsonIgnore]
    public string DisplayName =>
        string.IsNullOrWhiteSpace(PersonaName) ? GameName : $"{GameName} - {PersonaName}";

    [JsonIgnore]
    public string DisplayDetails
    {
        get
        {
            var accountLabel = string.IsNullOrWhiteSpace(AccountName) ? AppText.UnknownAccount : AccountName;
            return AppText.SteamAccountLabel(accountLabel, AccountId);
        }
    }

    [JsonIgnore]
    public string DisplayPath =>
        !string.IsNullOrWhiteSpace(EffectiveSourcePath)
            ? EffectiveSourcePath
            : Path.Combine("userdata", AccountId, "730");

    public GameSettingsBackup CloneForPackage()
    {
        return new GameSettingsBackup
        {
            Kind = Kind,
            GameName = GameName,
            AccountId = AccountId,
            SteamId64 = SteamId64,
            AccountName = AccountName,
            PersonaName = PersonaName,
            AvatarUrl = AvatarUrl,
            PackagePath = PackagePath,
            SourcePath = SourcePath,
            ResolvedPath = ResolvedPath,
            IsSelected = IsSelected
        };
    }
}
