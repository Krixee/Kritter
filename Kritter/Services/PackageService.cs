using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Kritter.Models;

namespace Kritter.Services;

public static class PackageService
{
    private const string PackageJsonEntry = "package.json";
    private const string SetupFilesFolder = "setup-files";
    private const string GameSettingsFolder = "game-settings";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task SavePackageAsync(string path, KritterPackage package)
    {
        package.CreatedAt = DateTime.UtcNow;
        package.Version = "1.2";

        var tempRoot = Path.Combine(Path.GetTempPath(), "Kritter", "PackageBuild", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            PrepareSetupInstallers(package, tempRoot);
            PrepareGameSettingsBackups(package, tempRoot);

            var json = JsonSerializer.Serialize(package, JsonOptions);
            var packageJsonPath = Path.Combine(tempRoot, PackageJsonEntry);
            await File.WriteAllTextAsync(packageJsonPath, json, Encoding.UTF8);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            ZipFile.CreateFromDirectory(tempRoot, path, CompressionLevel.Optimal, false);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, true);
                }
            }
            catch
            {
                // Best effort cleanup.
            }
        }
    }

    public static async Task<KritterPackage?> LoadPackageAsync(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        if (!IsZipPackage(path))
        {
            var legacyJson = await File.ReadAllTextAsync(path, Encoding.UTF8);
            var legacyPackage = JsonSerializer.Deserialize<KritterPackage>(legacyJson, JsonOptions);
            return EnsurePackageDefaults(legacyPackage);
        }

        using var archive = ZipFile.OpenRead(path);
        var packageEntry = archive.GetEntry(PackageJsonEntry);
        if (packageEntry == null)
        {
            return null;
        }

        using var stream = packageEntry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var json = await reader.ReadToEndAsync();

        var package = JsonSerializer.Deserialize<KritterPackage>(json, JsonOptions);
        package = EnsurePackageDefaults(package);
        if (package == null)
        {
            return null;
        }

        ExtractSetupInstallers(path, archive, package);
        ExtractGameSettingsBackups(path, archive, package);
        return package;
    }

    public static string GetDefaultSavePath()
    {
        var dir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        return Path.Combine(dir, $"kritter_paket_{DateTime.Now:yyyyMMdd_HHmm}.kritter");
    }

    private static void PrepareSetupInstallers(KritterPackage package, string tempRoot)
    {
        var setupDir = Path.Combine(tempRoot, SetupFilesFolder);
        Directory.CreateDirectory(setupDir);

        for (int i = 0; i < package.SetupInstallers.Count; i++)
        {
            var installer = package.SetupInstallers[i];
            var sourcePath = installer.EffectiveFilePath;
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                throw new FileNotFoundException($"Setup dosyasi bulunamadi: {installer.DisplayName}", sourcePath);
            }

            var sourceExtension = Path.GetExtension(sourcePath);
            var safeName = MakeSafeFileName(Path.GetFileNameWithoutExtension(sourcePath));
            var packagedFileName = $"{i + 1:D2}_{safeName}{sourceExtension}";
            var packageRelativePath = $"{SetupFilesFolder}/{packagedFileName}";
            var destinationPath = Path.Combine(setupDir, packagedFileName);

            File.Copy(sourcePath, destinationPath, true);

            installer.PackagePath = packageRelativePath.Replace('\\', '/');
            installer.FileName = Path.GetFileName(sourcePath);
        }
    }

    private static void PrepareGameSettingsBackups(KritterPackage package, string tempRoot)
    {
        var settingsDir = Path.Combine(tempRoot, GameSettingsFolder);
        Directory.CreateDirectory(settingsDir);

        for (int i = 0; i < package.GameSettingsBackups.Count; i++)
        {
            var backup = package.GameSettingsBackups[i];
            var sourcePath = backup.EffectiveSourcePath;
            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
            {
                throw new DirectoryNotFoundException($"Oyun ayari klasoru bulunamadi: {backup.DisplayName}");
            }

            var folderName = MakeSafeFileName($"{backup.Kind}_{backup.AccountId}_{backup.GameName}");
            var packagedFolderName = $"{i + 1:D2}_{folderName}";
            var packageRelativePath = $"{GameSettingsFolder}/{packagedFolderName}";
            var destinationPath = Path.Combine(settingsDir, packagedFolderName);

            CopyDirectory(sourcePath, destinationPath);

            backup.PackagePath = packageRelativePath.Replace('\\', '/');
        }
    }

    private static void ExtractSetupInstallers(string packagePath, ZipArchive archive, KritterPackage package)
    {
        if (package.SetupInstallers.Count == 0)
        {
            return;
        }

        var extractRoot = GetExtractionRoot(packagePath);
        if (Directory.Exists(extractRoot))
        {
            Directory.Delete(extractRoot, true);
        }

        Directory.CreateDirectory(extractRoot);

        foreach (var installer in package.SetupInstallers)
        {
            if (string.IsNullOrWhiteSpace(installer.PackagePath))
            {
                continue;
            }

            var entryPath = installer.PackagePath.Replace('\\', '/');
            var entry = archive.GetEntry(entryPath);
            if (entry == null)
            {
                continue;
            }

            var relativePath = entryPath.StartsWith($"{SetupFilesFolder}/", StringComparison.OrdinalIgnoreCase)
                ? entryPath[(SetupFilesFolder.Length + 1)..]
                : Path.GetFileName(entryPath);

            var destinationPath = Path.Combine(extractRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var destinationDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            entry.ExtractToFile(destinationPath, true);
            installer.ResolvedFilePath = destinationPath;

            if (string.IsNullOrWhiteSpace(installer.FileName))
            {
                installer.FileName = Path.GetFileName(destinationPath);
            }
        }
    }

    private static void ExtractGameSettingsBackups(string packagePath, ZipArchive archive, KritterPackage package)
    {
        if (package.GameSettingsBackups.Count == 0)
        {
            return;
        }

        var extractRoot = GetExtractionRoot(packagePath);
        Directory.CreateDirectory(extractRoot);

        foreach (var backup in package.GameSettingsBackups)
        {
            if (string.IsNullOrWhiteSpace(backup.PackagePath))
            {
                continue;
            }

            var entryPrefix = backup.PackagePath.Replace('\\', '/').TrimEnd('/') + "/";
            var matchingEntries = archive.Entries
                .Where(entry => entry.FullName.StartsWith(entryPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingEntries.Count == 0)
            {
                continue;
            }

            var destinationRoot = Path.Combine(extractRoot, backup.PackagePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(destinationRoot);

            foreach (var entry in matchingEntries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                var relativePath = entry.FullName[entryPrefix.Length..].Replace('/', Path.DirectorySeparatorChar);
                var destinationPath = Path.Combine(destinationRoot, relativePath);
                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrWhiteSpace(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                entry.ExtractToFile(destinationPath, true);
            }

            backup.ResolvedPath = destinationRoot;
        }
    }

    private static KritterPackage? EnsurePackageDefaults(KritterPackage? package)
    {
        if (package == null)
        {
            return null;
        }

        package.Apps ??= new System.Collections.Generic.List<WingetApp>();
        package.Fr33tyScripts ??= new System.Collections.Generic.List<OptimizationScript>();
        package.SetupInstallers ??= new System.Collections.Generic.List<SetupInstaller>();
        package.GameSettingsBackups ??= new System.Collections.Generic.List<GameSettingsBackup>();
        return package;
    }

    private static bool IsZipPackage(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length < 2)
        {
            return false;
        }

        int first = stream.ReadByte();
        int second = stream.ReadByte();
        return first == 'P' && second == 'K';
    }

    private static string GetExtractionRoot(string packagePath)
    {
        var packageName = Path.GetFileNameWithoutExtension(packagePath);
        var hash = ComputeFileHash(packagePath);
        return Path.Combine(Path.GetTempPath(), "Kritter", "ExtractedPackages", $"{packageName}_{hash}");
    }

    private static string ComputeFileHash(string path)
    {
        using var sha1 = SHA1.Create();
        using var stream = File.OpenRead(path);
        var hash = sha1.ComputeHash(stream);
        return Convert.ToHexString(hash).Substring(0, 12).ToLowerInvariant();
    }

    private static string MakeSafeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var chars = name.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (Array.IndexOf(invalidChars, chars[i]) >= 0)
            {
                chars[i] = '_';
            }
        }

        var safe = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "setup" : safe;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var directory in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, directory);
            Directory.CreateDirectory(Path.Combine(destinationDir, relativePath));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destinationPath = Path.Combine(destinationDir, relativePath);
            var destinationParent = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationParent))
            {
                Directory.CreateDirectory(destinationParent);
            }

            File.Copy(file, destinationPath, true);
        }
    }
}
