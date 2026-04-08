using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Kritter.Models;
using Microsoft.Win32;

namespace Kritter.Services;

public static class GameSettingsService
{
    private const ulong SteamId64Base = 76561197960265728;

    private static readonly HttpClient SteamHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(6)
    };

    public static async Task<List<SteamUserAccount>> DiscoverCs2AccountsAsync()
    {
        var steamPath = GetSteamInstallPath();
        if (string.IsNullOrWhiteSpace(steamPath))
        {
            return new List<SteamUserAccount>();
        }

        var userdataRoot = Path.Combine(steamPath, "userdata");
        if (!Directory.Exists(userdataRoot))
        {
            return new List<SteamUserAccount>();
        }

        var loginUsers = ParseLoginUsers(Path.Combine(steamPath, "config", "loginusers.vdf"));
        var accounts = new List<SteamUserAccount>();

        foreach (var userDir in Directory.GetDirectories(userdataRoot))
        {
            var accountId = Path.GetFileName(userDir);
            if (string.IsNullOrWhiteSpace(accountId) || !ulong.TryParse(accountId, out var accountIdValue))
            {
                continue;
            }

            var cs2Path = Path.Combine(userDir, "730");
            if (!Directory.Exists(cs2Path))
            {
                continue;
            }

            var steamId64 = (SteamId64Base + accountIdValue).ToString();
            loginUsers.TryGetValue(steamId64, out var loginUser);

            accounts.Add(new SteamUserAccount
            {
                AccountId = accountId,
                SteamId64 = steamId64,
                AccountName = loginUser?.AccountName ?? "",
                PersonaName = loginUser?.PersonaName ?? "",
                Cs2Path = cs2Path,
                IsMostRecent = loginUser?.IsMostRecent ?? false
            });
        }

        await EnrichSteamProfilesAsync(accounts);

        return accounts
            .OrderByDescending(a => a.IsMostRecent)
            .ThenBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static GameSettingsBackup CreateCs2Backup(SteamUserAccount account)
    {
        return new GameSettingsBackup
        {
            Kind = GameSettingsKind.Cs2,
            GameName = "Counter-Strike 2",
            AccountId = account.AccountId,
            SteamId64 = account.SteamId64,
            AccountName = account.AccountName,
            PersonaName = account.PersonaName,
            AvatarUrl = account.AvatarUrl,
            SourcePath = account.Cs2Path
        };
    }

    public static async Task<(bool Success, string Output)> RestoreAsync(GameSettingsBackup backup)
    {
        return backup.Kind switch
        {
            GameSettingsKind.Cs2 => await RestoreCs2Async(backup),
            _ => (false, "Desteklenmeyen oyun ayarı türü.")
        };
    }

    public static string? GetSteamInstallPath()
    {
        var candidates = new[]
        {
            Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string,
            Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string,
            Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null) as string,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam")
        };

        foreach (var candidate in candidates.Where(c => !string.IsNullOrWhiteSpace(c)))
        {
            var normalized = candidate!.Replace('/', Path.DirectorySeparatorChar);
            if (Directory.Exists(normalized))
            {
                return Path.GetFullPath(normalized);
            }
        }

        return null;
    }

    private static async Task<(bool Success, string Output)> RestoreCs2Async(GameSettingsBackup backup)
    {
        var sourcePath = backup.EffectiveSourcePath;
        if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
        {
            return (false, "CS2 ayar klasörü paketten çıkarılamadı.");
        }

        var steamPath = GetSteamInstallPath();
        if (string.IsNullOrWhiteSpace(steamPath))
        {
            return (false, "Steam bulunamadı. Lütfen önce Steam'i yükleyip en az bir kez giriş yapın.");
        }

        var userdataRoot = Path.GetFullPath(Path.Combine(steamPath, "userdata"));
        var targetPath = Path.GetFullPath(Path.Combine(userdataRoot, backup.AccountId, "730"));

        if (!targetPath.StartsWith(userdataRoot, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetFileName(targetPath), "730", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "CS2 hedef yolu doğrulanamadı.");
        }

        var parentDir = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrWhiteSpace(parentDir))
        {
            return (false, "CS2 hedef klasörü hazırlanamadı.");
        }

        Directory.CreateDirectory(parentDir);

        if (Directory.Exists(targetPath))
        {
            Directory.Delete(targetPath, true);
        }

        CopyDirectory(sourcePath, targetPath);
        await Task.CompletedTask;
        return (true, targetPath);
    }

    private static async Task EnrichSteamProfilesAsync(IEnumerable<SteamUserAccount> accounts)
    {
        var tasks = accounts.Select(async account =>
        {
            try
            {
                var profileXml = await SteamHttpClient.GetStringAsync($"https://steamcommunity.com/profiles/{account.SteamId64}/?xml=1");
                var doc = XDocument.Parse(profileXml);
                var root = doc.Root;
                if (root == null)
                {
                    return;
                }

                var personaName = root.Element("steamID")?.Value?.Trim();
                if (!string.IsNullOrWhiteSpace(personaName))
                {
                    account.PersonaName = personaName;
                }

                var avatarUrl = root.Element("avatarFull")?.Value?.Trim();
                if (!string.IsNullOrWhiteSpace(avatarUrl))
                {
                    account.AvatarUrl = avatarUrl;
                }
            }
            catch
            {
                // Ignore profile lookup failures; local loginusers.vdf data is enough to continue.
            }
        });

        await Task.WhenAll(tasks);
    }

    private static Dictionary<string, SteamLoginUser> ParseLoginUsers(string loginUsersPath)
    {
        var users = new Dictionary<string, SteamLoginUser>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(loginUsersPath))
        {
            return users;
        }

        var content = File.ReadAllText(loginUsersPath);
        var userRegex = new Regex("\"(?<id>\\d{17})\"\\s*\\{(?<body>.*?)\\}", RegexOptions.Singleline);
        var kvRegex = new Regex("\"(?<key>[^\"]+)\"\\s*\"(?<value>[^\"]*)\"");

        foreach (Match userMatch in userRegex.Matches(content))
        {
            var steamId64 = userMatch.Groups["id"].Value;
            var body = userMatch.Groups["body"].Value;
            var loginUser = new SteamLoginUser();

            foreach (Match kvMatch in kvRegex.Matches(body))
            {
                var key = kvMatch.Groups["key"].Value;
                var value = kvMatch.Groups["value"].Value;

                switch (key)
                {
                    case "AccountName":
                        loginUser.AccountName = value;
                        break;
                    case "PersonaName":
                        loginUser.PersonaName = value;
                        break;
                    case "MostRecent":
                        loginUser.IsMostRecent = value == "1";
                        break;
                }
            }

            users[steamId64] = loginUser;
        }

        return users;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        var sourceInfo = new DirectoryInfo(sourceDir);
        Directory.CreateDirectory(destinationDir);

        foreach (var directory in sourceInfo.GetDirectories("*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, directory.FullName);
            Directory.CreateDirectory(Path.Combine(destinationDir, relativePath));
        }

        foreach (var file in sourceInfo.GetFiles("*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file.FullName);
            var destinationPath = Path.Combine(destinationDir, relativePath);
            var destinationParent = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationParent))
            {
                Directory.CreateDirectory(destinationParent);
            }

            file.CopyTo(destinationPath, true);
        }
    }

    private sealed class SteamLoginUser
    {
        public string AccountName { get; set; } = "";
        public string PersonaName { get; set; } = "";
        public bool IsMostRecent { get; set; }
    }
}
