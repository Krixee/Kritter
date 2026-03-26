using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Kritter.Services;

public static class CleanupService
{
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromMinutes(10);

    public static async Task<string> RunCleanupAsync(Action<string>? onOutput = null)
    {
        var username = Environment.UserName;
        var tempPath = Environment.GetEnvironmentVariable("TEMP") ?? @$"C:\Users\{username}\AppData\Local\Temp";

        var script = $@"
$ErrorActionPreference = 'SilentlyContinue'

# Clean TEMP folder
Remove-Item -Path '{tempPath}\*' -Recurse -Force -ErrorAction SilentlyContinue
if (-not (Test-Path '{tempPath}')) {{ New-Item -ItemType Directory -Path '{tempPath}' -Force | Out-Null }}

# Clean user AppData Local Temp
$userTemp = ""C:\Users\{username}\AppData\Local\Temp""
Remove-Item -Path ""$userTemp\*"" -Recurse -Force -ErrorAction SilentlyContinue

# Clean user AppData Local Tmp
$userTmp = ""C:\Users\{username}\AppData\Local\Tmp""
Remove-Item -Path ""$userTmp\*"" -Recurse -Force -ErrorAction SilentlyContinue

# Clean Windows Recent
Remove-Item -Path ""C:\Windows\Recent\*"" -Force -ErrorAction SilentlyContinue

# Clean Windows Temp
Remove-Item -Path ""C:\Windows\Temp\*"" -Recurse -Force -ErrorAction SilentlyContinue

# Clean Recycle Bin
Remove-Item -Path ""C:\`$RECYCLE.BIN\*"" -Recurse -Force -ErrorAction SilentlyContinue

# Clean Prefetch
Remove-Item -Path ""C:\Windows\Prefetch\*"" -Force -ErrorAction SilentlyContinue

Write-Output 'Cleanup completed.'
";

        var tempScript = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"kritter_cleanup_{Guid.NewGuid():N}.ps1");
        try
        {
            await System.IO.File.WriteAllTextAsync(tempScript, script, Encoding.UTF8);

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScript}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            var output = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    output.AppendLine(e.Data);
                    onOutput?.Invoke(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            var waitTask = process.WaitForExitAsync();
            var completedTask = await Task.WhenAny(waitTask, Task.Delay(CleanupTimeout));
            if (completedTask != waitTask)
            {
                TryKillProcess(process);
                output.AppendLine($"Cleanup timeout: {CleanupTimeout.TotalMinutes:0} dakika.");
                return output.ToString();
            }

            await waitTask;

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"Cleanup error: {ex.Message}";
        }
        finally
        {
            try { System.IO.File.Delete(tempScript); } catch { }
        }
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
