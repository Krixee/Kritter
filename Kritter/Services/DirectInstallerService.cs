using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Kritter.Services;

public static class DirectInstallerService
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(30);
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(20)
    };

    public static async Task<(bool Success, string Output)> InstallAppAsync(string appName, string? installerUrl, string? silentArgs)
    {
        if (string.IsNullOrWhiteSpace(installerUrl))
        {
            return (false, "Direct installer URL is missing.");
        }

        string? tempFilePath = null;

        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "Kritter", "DirectInstallers");
            Directory.CreateDirectory(tempDir);

            var extension = GetInstallerExtension(installerUrl);
            var safeName = MakeSafeFileName(appName);
            tempFilePath = Path.Combine(tempDir, $"{safeName}_{Guid.NewGuid():N}{extension}");

            await DownloadInstallerAsync(installerUrl, tempFilePath);

            var (fileName, arguments) = BuildInstallerCommand(tempFilePath, extension, silentArgs);
            var output = await RunProcessAsync(fileName, arguments);
            var success = !output.Contains("error", StringComparison.OrdinalIgnoreCase);

            return (success, output);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(tempFilePath))
            {
                try { File.Delete(tempFilePath); } catch { }
            }
        }
    }

    private static async Task DownloadInstallerAsync(string url, string destinationPath)
    {
        using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await responseStream.CopyToAsync(fileStream);
    }

    private static (string FileName, string Arguments) BuildInstallerCommand(string filePath, string extension, string? silentArgs)
    {
        if (extension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
        {
            return ("msiexec.exe", $"/i \"{filePath}\" /qn /norestart");
        }

        var args = string.IsNullOrWhiteSpace(silentArgs) ? "/S" : silentArgs;
        return (filePath, args);
    }

    private static string GetInstallerExtension(string url)
    {
        try
        {
            var uri = new Uri(url);
            var ext = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(ext))
            {
                return ext.ToLowerInvariant();
            }
        }
        catch
        {
            // Use default below.
        }

        return ".exe";
    }

    private static string MakeSafeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            sb.Append(Array.IndexOf(invalidChars, ch) >= 0 ? '_' : ch);
        }

        var value = sb.ToString().Trim();
        return string.IsNullOrWhiteSpace(value) ? "app" : value;
    }

    private static async Task<string> RunProcessAsync(string fileName, string arguments)
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
            var timeoutMessage = $"Timeout: direct installer {ProcessTimeout.TotalMinutes:0} dakikada tamamlanmadi.";
            return string.IsNullOrWhiteSpace(timeoutError)
                ? $"{timeoutMessage}\n{timeoutOutput}".Trim()
                : $"{timeoutMessage}\n{timeoutOutput}\n{timeoutError}".Trim();
        }

        await waitTask;
        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode == 0)
        {
            return string.IsNullOrWhiteSpace(error) ? output : $"{output}\n{error}";
        }

        return string.IsNullOrWhiteSpace(error)
            ? $"ExitCode: {process.ExitCode}\n{output}"
            : $"ExitCode: {process.ExitCode}\n{output}\n{error}";
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
