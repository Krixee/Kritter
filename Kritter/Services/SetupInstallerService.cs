using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kritter.Models;

namespace Kritter.Services;

public static class SetupInstallerService
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(30);

    public static async Task<(bool Success, string Output)> InstallAsync(SetupInstaller installer)
    {
        var path = installer.EffectiveFilePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return (false, $"Setup dosyasi bulunamadi: {installer.DisplayName}");
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension == ".msi")
        {
            return await RunProcessAsync("msiexec.exe", $"/i \"{path}\" /qn /norestart");
        }

        if (extension is ".msix" or ".msixbundle" or ".appx" or ".appxbundle")
        {
            var command = $"Add-AppxPackage -Path '{path.Replace("'", "''")}'";
            return await RunProcessAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"");
        }

        if (extension != ".exe")
        {
            return (false, $"Desteklenmeyen setup turu: {extension}");
        }

        var argumentsToTry = BuildSilentArgsCandidates(installer.SilentArgs);
        var logs = new StringBuilder();

        foreach (var args in argumentsToTry)
        {
            var (success, output) = await RunProcessAsync(path, args);
            logs.AppendLine($"Args: {args}");
            logs.AppendLine(output);
            logs.AppendLine();

            if (success)
            {
                return (true, logs.ToString());
            }
        }

        return (false, logs.ToString());
    }

    private static List<string> BuildSilentArgsCandidates(string? preferredArgs)
    {
        var list = new List<string>();

        if (!string.IsNullOrWhiteSpace(preferredArgs))
        {
            list.Add(preferredArgs);
        }

        var defaults = new[]
        {
            "/S",
            "/silent",
            "/quiet",
            "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART"
        };

        foreach (var value in defaults)
        {
            if (!list.Any(existing => existing.Equals(value, StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(value);
            }
        }

        return list;
    }

    private static async Task<(bool Success, string Output)> RunProcessAsync(string fileName, string arguments)
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
            var timeoutMessage = $"Timeout: setup islemi {ProcessTimeout.TotalMinutes:0} dakikada tamamlanmadi.";
            var timeoutFull = string.IsNullOrWhiteSpace(timeoutError)
                ? $"{timeoutMessage}\n{timeoutOutput}"
                : $"{timeoutMessage}\n{timeoutOutput}\n{timeoutError}";
            return (false, timeoutFull.Trim());
        }

        await waitTask;
        var output = await outputTask;
        var error = await errorTask;

        var fullOutput = string.IsNullOrWhiteSpace(error) ? output : $"{output}\n{error}";
        return (process.ExitCode == 0, fullOutput);
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
