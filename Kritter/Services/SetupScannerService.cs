using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kritter.Models;

namespace Kritter.Services;

public static class SetupScannerService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe",
        ".msi",
        ".msix",
        ".msixbundle",
        ".appx",
        ".appxbundle"
    };

    private static readonly string[] NoiseTokens =
    {
        "setup", "installer", "install", "release", "portable", "x64", "x86", "win64", "win32",
        "latest", "offline", "online", "full", "client"
    };

    private static readonly Regex VersionRegex = new(@"\b\d+(\.\d+){1,4}\b", RegexOptions.Compiled);
    private static readonly Regex InvalidNameRegex = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    public static List<SetupInstaller> ScanFolder(string folderPath)
    {
        var results = new List<SetupInstaller>();
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return results;
        }

        var files = EnumerateFilesSafe(folderPath)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var filePath in files)
        {
            var fileName = Path.GetFileName(filePath);
            var displayName = GuessDisplayName(fileName);
            var extension = Path.GetExtension(filePath);

            results.Add(new SetupInstaller
            {
                DisplayName = displayName,
                FileName = fileName,
                SourceFilePath = filePath,
                SilentArgs = GuessSilentArgs(fileName, extension),
                IsSelected = true
            });
        }

        return results
            .OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> EnumerateFilesSafe(string root)
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
                files = Directory.EnumerateFiles(current);
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

    private static string GuessDisplayName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        name = VersionRegex.Replace(name, " ");
        name = InvalidNameRegex.Replace(name.ToLowerInvariant(), " ");

        foreach (var token in NoiseTokens)
        {
            name = Regex.Replace(name, $@"\b{Regex.Escape(token)}\b", " ", RegexOptions.IgnoreCase);
        }

        name = Regex.Replace(name, @"\s+", " ").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = Path.GetFileNameWithoutExtension(fileName);
        }

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
    }

    private static string? GuessSilentArgs(string fileName, string extension)
    {
        if (extension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var normalized = fileName.ToLowerInvariant();

        if (normalized.Contains("spotify"))
        {
            return "/silent";
        }

        if (normalized.Contains("riot"))
        {
            return "--silent";
        }

        if (normalized.Contains("quickshare") || normalized.Contains("quick share"))
        {
            return "/S";
        }

        return "/S";
    }
}
