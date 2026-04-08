using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Kritter.Models;
using Microsoft.Win32;

namespace Kritter.Services;

public static class WingetService
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(20);

    private sealed class RegistryInstalledApp
    {
        public string DisplayName { get; init; } = "";
        public string Publisher { get; init; } = "";
        public string InstallLocation { get; init; } = "";
        public string UninstallString { get; init; } = "";
        public string NormalizedDisplayName { get; init; } = "";
        public string SearchBlob { get; init; } = "";
    }

    private sealed class ReinstallDefinition
    {
        public ReinstallDefinition(
            string canonicalName,
            string[] matchTerms,
            string? wingetId = null,
            string? wingetLookupQuery = null,
            string? directInstallerUrl = null,
            string? directInstallerArgs = null)
        {
            CanonicalName = canonicalName;
            WingetId = wingetId;
            WingetLookupQuery = wingetLookupQuery;
            MatchTerms = matchTerms.Select(NormalizeText).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
            DirectInstallerUrl = directInstallerUrl;
            DirectInstallerArgs = directInstallerArgs;
        }

        public string CanonicalName { get; }
        public string[] MatchTerms { get; }
        public string? WingetId { get; }
        public string? WingetLookupQuery { get; }
        public string? DirectInstallerUrl { get; }
        public string? DirectInstallerArgs { get; }
    }

    private sealed class WingetCandidate
    {
        public string Name { get; init; } = "";
        public string Id { get; init; } = "";
    }

    private static readonly Regex MultiSpaceRegex = new(@"\s{2,}", RegexOptions.Compiled);
    private static readonly Regex NormalizeRegex = new(@"[^a-z0-9]+", RegexOptions.Compiled);
    private static readonly Regex VersionRegex = new(@"\b\d+(\.\d+){1,4}\b", RegexOptions.Compiled);

    private static readonly string[] StopTokens =
    {
        "x64", "x86", "64 bit", "32 bit", "64bit", "32bit", "machine wide", "launcher", "helper"
    };

    private static readonly string[] ExcludedNameFragments =
    {
        "windows app runtime",
        "gameinput",
        "gaming services",
        "edge webview",
        "webview2 runtime",
        "update health tools",
        "visual c",
        "redistributable",
        "desktop runtime",
        "asp net core runtime",
        "shared framework",
        "dotnet runtime",
        "dotnet sdk",
        "ui xaml",
        "physx",
        "chipset",
        "inno setup",
        "installer runtime",
        "microsoft edge",
        "app installer"
    };

    private static readonly string[] ExcludedIdPrefixes =
    {
        "microsoft windowsappruntime",
        "microsoft gameinput",
        "microsoft gamingservices",
        "microsoft edgewebview2runtime",
        "microsoft edge",
        "microsoft vcredist",
        "microsoft dotnet",
        "microsoft dotnet sdk",
        "microsoft aspnetcore",
        "microsoft vclibs",
        "microsoft ui xaml",
        "microsoft desktopappinstaller",
        "nvidia physx",
        "msix microsoft vclibs",
        "msix microsoft microsoftedge"
    };

    private static readonly List<ReinstallDefinition> ReinstallDefinitions = new()
    {
        new ReinstallDefinition(
            canonicalName: "Steam",
            wingetId: "Valve.Steam",
            matchTerms: new[] { "steam", "valve steam" },
            directInstallerUrl: "https://cdn.cloudflare.steamstatic.com/client/installer/SteamSetup.exe",
            directInstallerArgs: "/S"),

        new ReinstallDefinition(
            canonicalName: "Discord",
            wingetId: "Discord.Discord",
            matchTerms: new[] { "discord" },
            directInstallerUrl: "https://discord.com/api/download?platform=win&format=exe",
            directInstallerArgs: "/S"),

        new ReinstallDefinition(
            canonicalName: "Spotify",
            wingetId: "Spotify.Spotify",
            matchTerms: new[] { "spotify" },
            directInstallerUrl: "https://download.scdn.co/SpotifySetup.exe",
            directInstallerArgs: "/silent"),

        new ReinstallDefinition(
            canonicalName: "Riot Client",
            wingetId: "RiotGames.RiotClient",
            matchTerms: new[] { "riot client", "riot games", "valorant", "league of legends" }),

        new ReinstallDefinition(
            canonicalName: "Anytype",
            matchTerms: new[] { "anytype" },
            wingetLookupQuery: "Anytype"),

        new ReinstallDefinition(
            canonicalName: "Quick Share",
            matchTerms: new[] { "quick share", "samsung quick share" },
            wingetLookupQuery: "Quick Share"),

        new ReinstallDefinition(
            canonicalName: "Google Chrome",
            wingetId: "Google.Chrome",
            matchTerms: new[] { "google chrome", "chrome" },
            directInstallerUrl: "https://dl.google.com/dl/chrome/install/googlechromestandaloneenterprise64.msi"),

        new ReinstallDefinition(
            canonicalName: "Mozilla Firefox",
            wingetId: "Mozilla.Firefox",
            matchTerms: new[] { "mozilla firefox", "firefox" },
            directInstallerUrl: "https://download.mozilla.org/?product=firefox-latest&os=win64&lang=en-US",
            directInstallerArgs: "/S"),

        new ReinstallDefinition(
            canonicalName: "Telegram Desktop",
            wingetId: "Telegram.TelegramDesktop",
            matchTerms: new[] { "telegram desktop", "telegram" }),

        new ReinstallDefinition(
            canonicalName: "Visual Studio Code",
            wingetId: "Microsoft.VisualStudioCode",
            matchTerms: new[] { "visual studio code", "vs code", "vscode" }),

        new ReinstallDefinition(
            canonicalName: "7-Zip",
            wingetId: "7zip.7zip",
            matchTerms: new[] { "7 zip", "7zip" }),

        new ReinstallDefinition(
            canonicalName: "VLC",
            wingetId: "VideoLAN.VLC",
            matchTerms: new[] { "vlc", "videolan" }),

        new ReinstallDefinition(
            canonicalName: "OBS Studio",
            wingetId: "OBSProject.OBSStudio",
            matchTerms: new[] { "obs studio", "open broadcaster" })
    };

    public static async Task<List<WingetApp>> ScanInstalledAppsAsync()
    {
        try
        {
            var registryApps = GetRegistryInstalledApps();
            var startMenuSignals = GetStartMenuSignals();
            var installedWingetApps = await GetWingetInstalledAppsAsync();

            var dynamicDefinitionWingetIds = await ResolveDefinitionWingetIdsAsync(registryApps, startMenuSignals);
            var genericWingetMatches = await ResolveGenericWingetMatchesAsync(registryApps, dynamicDefinitionWingetIds);

            return BuildReinstallableApps(
                registryApps,
                startMenuSignals,
                installedWingetApps,
                dynamicDefinitionWingetIds,
                genericWingetMatches);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Reinstallable scan error: {ex.Message}");
            return new List<WingetApp>();
        }
    }

    public static List<WingetApp> GetCommonApps(List<WingetApp> installedApps)
    {
        var installedIds = new HashSet<string>(
            installedApps
                .Where(a => a.InstallMethod == InstallMethod.Winget && !string.IsNullOrWhiteSpace(a.Id))
                .Select(a => a.Id),
            StringComparer.OrdinalIgnoreCase);

        var installedNames = new HashSet<string>(
            installedApps.Select(a => NormalizeText(a.Name)),
            StringComparer.OrdinalIgnoreCase);

        var commonApps = new List<WingetApp>();
        foreach (var definition in ReinstallDefinitions)
        {
            if (string.IsNullOrWhiteSpace(definition.WingetId))
            {
                continue;
            }

            if (installedIds.Contains(definition.WingetId) ||
                installedNames.Contains(NormalizeText(definition.CanonicalName)))
            {
                continue;
            }

            commonApps.Add(new WingetApp
            {
                Id = definition.WingetId,
                Name = definition.CanonicalName,
                InstallMethod = InstallMethod.Winget,
                DirectInstallerUrl = definition.DirectInstallerUrl,
                DirectInstallerArgs = definition.DirectInstallerArgs,
                IsInstalled = false,
                IsSelected = false
            });
        }

        return commonApps.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static Task<(bool Success, string Output)> InstallAppAsync(WingetApp app)
    {
        if (app.InstallMethod == InstallMethod.Direct)
        {
            return DirectInstallerService.InstallAppAsync(app.Name, app.DirectInstallerUrl, app.DirectInstallerArgs);
        }

        if (string.IsNullOrWhiteSpace(app.Id))
        {
            return Task.FromResult((false, "Winget app id is empty."));
        }

        return InstallAppAsync(app.Id);
    }

    public static async Task<(bool Success, string Output)> InstallAppAsync(string wingetId)
    {
        try
        {
            var output = await RunProcessAsync("winget",
                $"install --id {wingetId} --accept-source-agreements --accept-package-agreements --silent --disable-interactivity");

            bool success = !output.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                           output.Contains("successfully", StringComparison.OrdinalIgnoreCase) ||
                           output.Contains("already installed", StringComparison.OrdinalIgnoreCase);
            return (success, output);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static async Task<List<WingetApp>> GetWingetInstalledAppsAsync()
    {
        try
        {
            var output = await RunProcessAsync("winget", "list --accept-source-agreements --disable-interactivity");
            return ParseWingetInstalledOutput(output);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Winget list scan failed: {ex.Message}");
            return new List<WingetApp>();
        }
    }

    private static async Task<Dictionary<string, string>> ResolveDefinitionWingetIdsAsync(
        List<RegistryInstalledApp> registryApps,
        HashSet<string> startMenuSignals)
    {
        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in ReinstallDefinitions.Where(d => string.IsNullOrWhiteSpace(d.WingetId) && !string.IsNullOrWhiteSpace(d.WingetLookupQuery)))
        {
            bool installedByHeuristic = registryApps.Any(r =>
                definition.MatchTerms.Any(term => r.SearchBlob.Contains(term, StringComparison.OrdinalIgnoreCase))) ||
                startMenuSignals.Any(s => definition.MatchTerms.Any(term => s.Contains(term, StringComparison.OrdinalIgnoreCase)));

            if (!installedByHeuristic)
            {
                continue;
            }

            var id = await ResolveWingetIdByQueryAsync(definition.WingetLookupQuery!, definition.CanonicalName);
            if (!string.IsNullOrWhiteSpace(id))
            {
                resolved[definition.CanonicalName] = id;
            }
        }

        return resolved;
    }

    private static async Task<Dictionary<string, WingetCandidate>> ResolveGenericWingetMatchesAsync(
        List<RegistryInstalledApp> registryApps,
        Dictionary<string, string> definitionResolvedWingetIds)
    {
        var resolved = new Dictionary<string, WingetCandidate>(StringComparer.OrdinalIgnoreCase);

        var knownNames = new HashSet<string>(
            ReinstallDefinitions.Select(d => NormalizeText(d.CanonicalName)),
            StringComparer.OrdinalIgnoreCase);

        var candidates = registryApps
            .Where(a =>
                !knownNames.Contains(a.NormalizedDisplayName) &&
                !a.SearchBlob.Contains("microsoft", StringComparison.OrdinalIgnoreCase) &&
                !a.SearchBlob.Contains("visual c++", StringComparison.OrdinalIgnoreCase) &&
                !a.SearchBlob.Contains("redistributable", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.DisplayName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

        foreach (var displayName in candidates)
        {
            if (definitionResolvedWingetIds.Keys.Any(k =>
                    NormalizeText(k).Equals(NormalizeText(displayName), StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var foundId = await ResolveWingetIdByQueryAsync(displayName, displayName);
            if (string.IsNullOrWhiteSpace(foundId))
            {
                continue;
            }

            resolved[NormalizeText(displayName)] = new WingetCandidate
            {
                Name = CleanDisplayName(displayName),
                Id = foundId
            };
        }

        return resolved;
    }

    private static async Task<string?> ResolveWingetIdByQueryAsync(string query, string expectedName)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        try
        {
            var output = await RunProcessAsync("winget",
                $"search --query \"{query}\" --source winget --accept-source-agreements --disable-interactivity");

            var candidates = ParseWingetSearchOutput(output);
            if (candidates.Count == 0)
            {
                return null;
            }

            var expected = NormalizeText(expectedName);
            var best = candidates
                .Select(c => new { Candidate = c, Score = ScoreWingetCandidate(expected, c) })
                .OrderByDescending(v => v.Score)
                .FirstOrDefault();

            return best != null && best.Score >= 25 ? best.Candidate.Id : null;
        }
        catch
        {
            return null;
        }
    }

    private static int ScoreWingetCandidate(string expectedNormalized, WingetCandidate candidate)
    {
        var nameNorm = NormalizeText(candidate.Name);
        var idNorm = NormalizeText(candidate.Id);

        int score = 0;
        if (nameNorm == expectedNormalized)
        {
            score += 80;
        }

        if (nameNorm.Contains(expectedNormalized, StringComparison.OrdinalIgnoreCase))
        {
            score += 35;
        }

        if (expectedNormalized.Contains(nameNorm, StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
        }

        if (idNorm.Contains(expectedNormalized, StringComparison.OrdinalIgnoreCase))
        {
            score += 25;
        }

        var expectedTokens = expectedNormalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var nameTokens = nameNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int overlap = expectedTokens.Intersect(nameTokens, StringComparer.OrdinalIgnoreCase).Count();
        score += overlap * 12;

        return score;
    }

    private static List<WingetApp> BuildReinstallableApps(
        List<RegistryInstalledApp> registryApps,
        HashSet<string> startMenuSignals,
        List<WingetApp> installedWingetApps,
        Dictionary<string, string> dynamicDefinitionWingetIds,
        Dictionary<string, WingetCandidate> genericWingetMatches)
    {
        var result = new Dictionary<string, WingetApp>(StringComparer.OrdinalIgnoreCase);

        foreach (var wingetApp in installedWingetApps)
        {
            var definition = FindDefinitionByWingetId(wingetApp.Id) ?? FindDefinitionByText(wingetApp.Name);
            var canonicalName = definition?.CanonicalName ?? CleanDisplayName(wingetApp.Name);
            var key = BuildAppKey(wingetApp.Id, canonicalName);

            if (result.ContainsKey(key))
            {
                continue;
            }

            result[key] = new WingetApp
            {
                Id = wingetApp.Id,
                Name = canonicalName,
                InstallMethod = InstallMethod.Winget,
                DirectInstallerUrl = definition?.DirectInstallerUrl,
                DirectInstallerArgs = definition?.DirectInstallerArgs,
                IsInstalled = true,
                IsSelected = true
            };
        }

        foreach (var registryApp in registryApps)
        {
            var definition = FindMatchingDefinition(registryApp, startMenuSignals);

            if (definition != null)
            {
                var wingetId = definition.WingetId;
                if (string.IsNullOrWhiteSpace(wingetId) &&
                    dynamicDefinitionWingetIds.TryGetValue(definition.CanonicalName, out var dynamicId))
                {
                    wingetId = dynamicId;
                }

                bool hasWinget = !string.IsNullOrWhiteSpace(wingetId);
                bool hasDirect = !string.IsNullOrWhiteSpace(definition.DirectInstallerUrl);
                if (!hasWinget && !hasDirect)
                {
                    continue;
                }

                var method = hasWinget ? InstallMethod.Winget : InstallMethod.Direct;
                var key = BuildAppKey(wingetId, definition.CanonicalName);
                if (result.ContainsKey(key))
                {
                    continue;
                }

                result[key] = new WingetApp
                {
                    Id = hasWinget ? wingetId! : definition.CanonicalName,
                    Name = definition.CanonicalName,
                    InstallMethod = method,
                    DirectInstallerUrl = definition.DirectInstallerUrl,
                    DirectInstallerArgs = definition.DirectInstallerArgs,
                    IsInstalled = true,
                    IsSelected = true
                };

                continue;
            }

            if (!genericWingetMatches.TryGetValue(registryApp.NormalizedDisplayName, out var candidate))
            {
                continue;
            }

            var genericKey = BuildAppKey(candidate.Id, candidate.Name);
            if (result.ContainsKey(genericKey))
            {
                continue;
            }

            result[genericKey] = new WingetApp
            {
                Id = candidate.Id,
                Name = candidate.Name,
                InstallMethod = InstallMethod.Winget,
                IsInstalled = true,
                IsSelected = true
            };
        }

        return result.Values
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ReinstallDefinition? FindMatchingDefinition(RegistryInstalledApp app, HashSet<string> startMenuSignals)
    {
        foreach (var definition in ReinstallDefinitions)
        {
            foreach (var term in definition.MatchTerms)
            {
                if (app.SearchBlob.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    return definition;
                }
            }
        }

        var appTokens = app.NormalizedDisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var signal in startMenuSignals)
        {
            var signalTokens = signal.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int overlap = appTokens.Intersect(signalTokens, StringComparer.OrdinalIgnoreCase).Count();
            if (overlap < 2)
            {
                continue;
            }

            var definition = FindDefinitionByText(signal);
            if (definition != null)
            {
                return definition;
            }
        }

        return null;
    }

    private static ReinstallDefinition? FindDefinitionByWingetId(string wingetId)
    {
        return ReinstallDefinitions.FirstOrDefault(d =>
            !string.IsNullOrWhiteSpace(d.WingetId) &&
            d.WingetId.Equals(wingetId, StringComparison.OrdinalIgnoreCase));
    }

    private static ReinstallDefinition? FindDefinitionByText(string text)
    {
        var normalized = NormalizeText(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return ReinstallDefinitions.FirstOrDefault(d =>
            d.MatchTerms.Any(term => normalized.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    private static List<RegistryInstalledApp> GetRegistryInstalledApps()
    {
        var rawApps = new List<RegistryInstalledApp>();
        ReadUninstallKey(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", rawApps);
        ReadUninstallKey(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", rawApps);
        ReadUninstallKey(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", rawApps);

        return rawApps
            .GroupBy(a => a.NormalizedDisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(v => v.SearchBlob.Length).First())
            .ToList();
    }

    private static void ReadUninstallKey(RegistryKey hive, string subKeyPath, List<RegistryInstalledApp> target)
    {
        try
        {
            using var key = hive.OpenSubKey(subKeyPath);
            if (key == null)
            {
                return;
            }

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                try
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    if (subKey == null)
                    {
                        continue;
                    }

                    var displayName = (subKey.GetValue("DisplayName") as string)?.Trim();
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        continue;
                    }

                    if (GetIntRegistryValue(subKey.GetValue("SystemComponent")) == 1)
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(subKey.GetValue("ParentKeyName") as string))
                    {
                        continue;
                    }

                    if (displayName.StartsWith("Security Update", StringComparison.OrdinalIgnoreCase) ||
                        displayName.StartsWith("Update for", StringComparison.OrdinalIgnoreCase) ||
                        displayName.StartsWith("Hotfix", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var publisher = (subKey.GetValue("Publisher") as string)?.Trim() ?? string.Empty;
                    var installLocation = (subKey.GetValue("InstallLocation") as string)?.Trim() ?? string.Empty;
                    var uninstallString = (subKey.GetValue("UninstallString") as string)?.Trim() ?? string.Empty;
                    var normalizedDisplay = NormalizeText(displayName);

                    if (string.IsNullOrWhiteSpace(normalizedDisplay))
                    {
                        continue;
                    }

                    if (ShouldExcludeInstalledApp(displayName, subKeyName, publisher, installLocation, uninstallString))
                    {
                        continue;
                    }

                    var searchBlob = NormalizeText($"{displayName} {publisher} {installLocation} {uninstallString}");

                    target.Add(new RegistryInstalledApp
                    {
                        DisplayName = CleanDisplayName(displayName),
                        Publisher = publisher,
                        InstallLocation = installLocation,
                        UninstallString = uninstallString,
                        NormalizedDisplayName = normalizedDisplay,
                        SearchBlob = searchBlob
                    });
                }
                catch
                {
                    // Continue scanning.
                }
            }
        }
        catch
        {
            // Continue scanning.
        }
    }

    private static int GetIntRegistryValue(object? value)
    {
        if (value == null)
        {
            return 0;
        }

        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return 0;
        }
    }

    private static HashSet<string> GetStartMenuSignals()
    {
        var signals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)
        };

        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                continue;
            }

            foreach (var shortcut in EnumerateFilesSafe(root, "*.lnk"))
            {
                var signal = NormalizeText(Path.GetFileNameWithoutExtension(shortcut));
                if (!string.IsNullOrWhiteSpace(signal))
                {
                    signals.Add(signal);
                }
            }
        }

        return signals;
    }

    private static IEnumerable<string> EnumerateFilesSafe(string root, string pattern)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            IEnumerable<string> directories;
            IEnumerable<string> files;

            try
            {
                directories = Directory.EnumerateDirectories(current);
            }
            catch
            {
                directories = Array.Empty<string>();
            }

            try
            {
                files = Directory.EnumerateFiles(current, pattern);
            }
            catch
            {
                files = Array.Empty<string>();
            }

            foreach (var file in files)
            {
                yield return file;
            }

            foreach (var dir in directories)
            {
                pending.Push(dir);
            }
        }
    }

    private static List<WingetApp> ParseWingetInstalledOutput(string output)
    {
        var parsed = ParseWingetTable(output);
        var result = new List<WingetApp>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in parsed)
        {
            if (string.IsNullOrWhiteSpace(item.Id) || !seen.Add(item.Id))
            {
                continue;
            }

            if (ShouldExcludeInstalledApp(item.Name, item.Id, publisher: null))
            {
                continue;
            }

            result.Add(new WingetApp
            {
                Id = item.Id,
                Name = item.Name,
                InstallMethod = InstallMethod.Winget,
                IsInstalled = true,
                IsSelected = true
            });
        }

        return result;
    }

    private static List<WingetCandidate> ParseWingetSearchOutput(string output)
    {
        return ParseWingetTable(output);
    }

    private static List<WingetCandidate> ParseWingetTable(string output)
    {
        var result = new List<WingetCandidate>();
        if (string.IsNullOrWhiteSpace(output))
        {
            return result;
        }

        var lines = output
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd())
            .ToArray();

        int separatorIndex = Array.FindIndex(lines, IsSeparatorLine);
        int dataStart = separatorIndex >= 0 ? separatorIndex + 1 : 0;

        for (int i = dataStart; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("Name", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Ad", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("---", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var cols = MultiSpaceRegex.Split(line)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToArray();

            if (cols.Length < 2)
            {
                continue;
            }

            var name = cols[0].Trim();
            var id = cols[1].Trim();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            if (id.Contains(' ') || id.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(new WingetCandidate
            {
                Name = name,
                Id = id
            });
        }

        return result;
    }

    private static bool IsSeparatorLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var trimmed = line.Trim();
        return trimmed.Length >= 10 && trimmed.All(ch => ch == '-' || ch == ' ');
    }

    private static string BuildAppKey(string? wingetId, string appName)
    {
        if (!string.IsNullOrWhiteSpace(wingetId))
        {
            return $"id:{wingetId.Trim().ToLowerInvariant()}";
        }

        return $"name:{NormalizeText(appName)}";
    }

    private static string CleanDisplayName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "";
        }

        var cleaned = VersionRegex.Replace(input, " ");
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        return cleaned;
    }

    private static bool ShouldExcludeInstalledApp(
        string? name,
        string? idOrKey,
        string? publisher,
        string? installLocation = null,
        string? uninstallString = null)
    {
        var lowerRawId = idOrKey?.Trim().ToLowerInvariant() ?? string.Empty;
        var normalizedName = NormalizeText(name);
        var normalizedId = NormalizeText(idOrKey);
        var normalizedPublisher = NormalizeText(publisher);
        var normalizedLocation = NormalizeText(installLocation);
        var normalizedUninstall = NormalizeText(uninstallString);

        if (string.IsNullOrWhiteSpace(normalizedName) && string.IsNullOrWhiteSpace(normalizedId))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(idOrKey) && FindDefinitionByWingetId(idOrKey) != null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(name) && FindDefinitionByText(name) != null)
        {
            return false;
        }

        if (ExcludedIdPrefixes.Any(prefix =>
                normalizedId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (lowerRawId.StartsWith("microsoft.windowsappruntime", StringComparison.OrdinalIgnoreCase) ||
            lowerRawId.StartsWith("microsoft.gameinput", StringComparison.OrdinalIgnoreCase) ||
            lowerRawId.StartsWith("microsoft.gamingservices", StringComparison.OrdinalIgnoreCase) ||
            lowerRawId.StartsWith("microsoft.edgewebview2runtime", StringComparison.OrdinalIgnoreCase) ||
            lowerRawId.StartsWith("microsoft.dotnet", StringComparison.OrdinalIgnoreCase) ||
            lowerRawId.StartsWith("microsoft.ui.xaml", StringComparison.OrdinalIgnoreCase) ||
            lowerRawId.StartsWith("microsoft.vclibs", StringComparison.OrdinalIgnoreCase) ||
            lowerRawId.StartsWith("microsoft.desktopappinstaller", StringComparison.OrdinalIgnoreCase) ||
            lowerRawId.StartsWith("nvidia.physx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (ExcludedNameFragments.Any(fragment =>
                normalizedName.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (normalizedPublisher.Contains("microsoft", StringComparison.OrdinalIgnoreCase) &&
            (normalizedName.Contains("runtime", StringComparison.OrdinalIgnoreCase) ||
             normalizedName.Contains("sdk", StringComparison.OrdinalIgnoreCase) ||
             normalizedName.Contains("framework", StringComparison.OrdinalIgnoreCase) ||
             normalizedName.Contains("redistributable", StringComparison.OrdinalIgnoreCase) ||
             normalizedName.Contains("webview", StringComparison.OrdinalIgnoreCase) ||
             normalizedName.Contains("xaml", StringComparison.OrdinalIgnoreCase) ||
             normalizedName.Contains("gameinput", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if ((normalizedPublisher.Contains("advanced micro devices", StringComparison.OrdinalIgnoreCase) ||
             normalizedPublisher.Equals("amd", StringComparison.OrdinalIgnoreCase) ||
             normalizedPublisher.Contains("amd", StringComparison.OrdinalIgnoreCase)) &&
            normalizedName.Contains("chipset", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalizedPublisher.Contains("nvidia", StringComparison.OrdinalIgnoreCase) &&
            normalizedName.Contains("physx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalizedLocation.Contains("windowsapps", StringComparison.OrdinalIgnoreCase) &&
            normalizedPublisher.Contains("microsoft", StringComparison.OrdinalIgnoreCase) &&
            (normalizedName.Contains("runtime", StringComparison.OrdinalIgnoreCase) ||
             normalizedName.Contains("sdk", StringComparison.OrdinalIgnoreCase) ||
             normalizedName.Contains("framework", StringComparison.OrdinalIgnoreCase) ||
             normalizedName.Contains("xaml", StringComparison.OrdinalIgnoreCase) ||
             normalizedName.Contains("webview", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return normalizedUninstall.Contains("windowsappruntime", StringComparison.OrdinalIgnoreCase) ||
               normalizedUninstall.Contains("gameinput", StringComparison.OrdinalIgnoreCase) ||
               normalizedUninstall.Contains("webview", StringComparison.OrdinalIgnoreCase) ||
               normalizedUninstall.Contains("dotnet", StringComparison.OrdinalIgnoreCase) ||
               normalizedUninstall.Contains("xaml", StringComparison.OrdinalIgnoreCase) ||
               normalizedUninstall.Contains("physx", StringComparison.OrdinalIgnoreCase) ||
               normalizedUninstall.Contains("chipset", StringComparison.OrdinalIgnoreCase) ||
               normalizedUninstall.Contains("inno setup", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var text = input.Trim().ToLowerInvariant();
        text = VersionRegex.Replace(text, " ");

        foreach (var token in StopTokens)
        {
            text = Regex.Replace(text, $@"\b{Regex.Escape(token)}\b", " ");
        }

        text = NormalizeRegex.Replace(text, " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }

    public static async Task<string> RunProcessAsync(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi };
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        var waitTask = process.WaitForExitAsync();
        var completedTask = await Task.WhenAny(waitTask, Task.Delay(ProcessTimeout));

        if (completedTask != waitTask)
        {
            TryKillProcess(process);
            var timeoutOutput = await outputTask;
            var timeoutError = await errorTask;
            var timeoutMessage = $"Timeout: islem {ProcessTimeout.TotalMinutes:0} dakikada tamamlanmadi.";
            return string.IsNullOrWhiteSpace(timeoutError)
                ? $"{timeoutMessage}\n{timeoutOutput}".Trim()
                : $"{timeoutMessage}\n{timeoutOutput}\n{timeoutError}".Trim();
        }

        await waitTask;
        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            return string.IsNullOrWhiteSpace(error)
                ? $"ExitCode: {process.ExitCode}\n{output}"
                : $"ExitCode: {process.ExitCode}\n{output}\n{error}";
        }

        return string.IsNullOrWhiteSpace(error) ? output : $"{output}\n{error}";
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort.
        }
    }
}
