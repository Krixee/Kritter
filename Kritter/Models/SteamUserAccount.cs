namespace Kritter.Models;

public class SteamUserAccount
{
    public string AccountId { get; set; } = "";
    public string SteamId64 { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string PersonaName { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string Cs2Path { get; set; } = "";
    public bool IsMostRecent { get; set; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(PersonaName) ? AccountId : PersonaName;

    public string DisplaySubtitle
    {
        get
        {
            var loginName = string.IsNullOrWhiteSpace(AccountName) ? "bilinmiyor" : AccountName;
            return $"Steam kullanıcı adı: {loginName} | userdata/{AccountId}";
        }
    }
}
