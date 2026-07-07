using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Kritter.Localization;

namespace Kritter.Services;

public sealed class UpdateCheckResult
{
    public bool UpdateAvailable { get; init; }
    public string LatestVersion { get; init; } = "";
    public string ReleaseUrl { get; init; } = "";
}

/// <summary>
/// Checks the GitHub releases API on startup and reports whether a newer version than the
/// currently running build is published. All failures are swallowed so a missing network
/// connection, a rate limit, or a repository without releases never blocks or breaks start-up.
/// </summary>
public static class UpdateService
{
    private const string GitHubOwner = "Krixee";
    private const string GitHubRepo = "Kritter";

    public static string ReleasesUrl => $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases/latest";
    private static string LatestReleaseApiUrl =>
        $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";

    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        // GitHub requires a User-Agent header on all API requests.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Kritter-UpdateCheck");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public static async Task<UpdateCheckResult?> CheckForUpdatesAsync()
    {
        try
        {
            var json = await Http.GetStringAsync(LatestReleaseApiUrl);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tag = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(tag))
            {
                return null;
            }

            var releaseUrl = root.TryGetProperty("html_url", out var urlProp)
                ? urlProp.GetString() ?? ReleasesUrl
                : ReleasesUrl;

            var latest = ParseVersion(tag);
            var current = ParseVersion(AppText.AppVersion);

            return new UpdateCheckResult
            {
                UpdateAvailable = CompareVersions(latest, current) > 0,
                LatestVersion = tag.Trim(),
                ReleaseUrl = releaseUrl
            };
        }
        catch
        {
            // Offline, rate limited, no releases yet, etc. Never surface as an error.
            return null;
        }
    }

    /// <summary>
    /// Extracts up to four numeric components from a tag such as "Kritter v3", "v3.0.1" or "3.1".
    /// </summary>
    private static int[] ParseVersion(string raw)
    {
        var digits = new StringBuilder();
        foreach (var ch in raw)
        {
            if (char.IsDigit(ch) || ch == '.')
            {
                digits.Append(ch);
            }
            else if (digits.Length > 0 && char.IsWhiteSpace(ch) && digits[^1] != '.')
            {
                // Stop at the first break after we started collecting a number so trailing
                // words ("beta", dates, ...) do not pollute the version.
                break;
            }
        }

        var parts = digits.ToString()
            .Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => int.TryParse(p, out var n) ? n : 0)
            .ToArray();

        return parts.Length > 0 ? parts : new[] { 0 };
    }

    private static int CompareVersions(int[] a, int[] b)
    {
        int length = Math.Max(a.Length, b.Length);
        for (int i = 0; i < length; i++)
        {
            int av = i < a.Length ? a[i] : 0;
            int bv = i < b.Length ? b[i] : 0;
            if (av != bv)
            {
                return av.CompareTo(bv);
            }
        }

        return 0;
    }

    public static void OpenReleasePage(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = string.IsNullOrWhiteSpace(url) ? ReleasesUrl : url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Best effort.
        }
    }
}
