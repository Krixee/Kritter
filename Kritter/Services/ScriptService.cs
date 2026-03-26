using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Kritter.Models;

namespace Kritter.Services;

public static class ScriptService
{
    private static readonly TimeSpan ScriptExecutionTimeout = TimeSpan.FromMinutes(45);
    private static readonly TimeSpan CommandExecutionTimeout = TimeSpan.FromMinutes(15);
    private static string ScriptsRoot => Path.Combine(AppContext.BaseDirectory, "Scripts");
    private static string KritterPath => Path.Combine(ScriptsRoot, "Kritter Recommended");
    private static string Fr33tyPath => Path.Combine(ScriptsRoot, "fr33ty Recommended");

    // Bloatware script uses choice 2 for "Remove All Bloatware (Recommended)"
    private static readonly Dictionary<string, int> SpecialChoiceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "15 Bloatware.ps1", 2 }
    };

    public static List<OptimizationScript> GetKritterRecommendedScripts()
    {
        var scripts = new List<OptimizationScript>();
        if (!Directory.Exists(KritterPath)) return scripts;

        foreach (var file in Directory.GetFiles(KritterPath))
        {
            var fileName = Path.GetFileName(file);
            var script = ParseScriptFile(file, fileName, "Kritter Recommended");
            if (script != null)
                scripts.Add(script);
        }

        return scripts.OrderBy(s => s.Is00Item).ThenBy(s => s.SortOrder).ToList();
    }

    public static List<(string Category, List<OptimizationScript> Scripts)> GetFr33tyScripts()
    {
        var categories = new List<(string, List<OptimizationScript>)>();
        if (!Directory.Exists(Fr33tyPath)) return categories;

        var dirs = Directory.GetDirectories(Fr33tyPath)
            .Where(d => !Path.GetFileName(d).Equals("scripts", StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => Path.GetFileName(d))
            .ToList();

        foreach (var dir in dirs)
        {
            var categoryName = Path.GetFileName(dir);
            var scripts = new List<OptimizationScript>();

            foreach (var file in Directory.GetFiles(dir))
            {
                var fileName = Path.GetFileName(file);
                var relativePath = Path.GetRelativePath(Fr33tyPath, file);
                var script = ParseScriptFile(file, fileName, categoryName);
                if (script != null)
                {
                    script.RelativePath = relativePath;
                    scripts.Add(script);
                }
            }

            if (scripts.Count > 0)
            {
                categories.Add((categoryName, scripts.OrderBy(s => s.SortOrder).ToList()));
            }
        }

        return categories;
    }

    private static OptimizationScript? ParseScriptFile(string fullPath, string fileName, string category)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var fileType = ext switch
        {
            ".ps1" => ScriptFileType.PowerShell,
            ".cmd" => ScriptFileType.Cmd,
            ".url" => ScriptFileType.Url,
            ".lnk" => ScriptFileType.Lnk,
            _ => (ScriptFileType?)null
        };

        if (fileType == null) return null;

        var match = Regex.Match(fileName, @"^(\d+)\s+(.+)\.\w+$");
        if (!match.Success) return null;

        int sortOrder = int.Parse(match.Groups[1].Value);
        string displayName = match.Groups[2].Value;
        bool is00 = fileName.StartsWith("00");

        return new OptimizationScript
        {
            FullPath = fullPath,
            RelativePath = Path.GetRelativePath(ScriptsRoot, fullPath),
            DisplayName = displayName,
            Category = category,
            SortOrder = sortOrder,
            Is00Item = is00,
            FileType = fileType.Value,
            IsSelected = true
        };
    }

    public static string ExtractChoiceBlock(string scriptContent, int choice)
    {
        var lines = scriptContent.Split('\n');
        bool foundSwitch = false;
        bool inTargetBlock = false;
        int braceDepth = 0;
        var blockLines = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();

            if (!foundSwitch)
            {
                if (trimmed.Contains("switch") && trimmed.Contains("$choice"))
                {
                    foundSwitch = true;
                }
                continue;
            }

            if (!inTargetBlock)
            {
                if (Regex.IsMatch(trimmed, $@"^{choice}\s*\{{"))
                {
                    inTargetBlock = true;
                    braceDepth = 1;
                    continue;
                }
                continue;
            }

            int openBraces = lines[i].Count(c => c == '{');
            int closeBraces = lines[i].Count(c => c == '}');
            braceDepth += openBraces - closeBraces;

            if (braceDepth <= 0)
                break;

            blockLines.Add(lines[i]);
        }

        return CleanInteractiveElements(string.Join("\n", blockLines));
    }

    private static string CleanInteractiveElements(string code)
    {
        // Phase 1: Replace interactive uninstall + polling blocks with safe non-interactive versions.
        // Pattern: Start-Process "X" -ArgumentList "/Uninstall" ... $processExists ... do { } while ... } else { }
        // These infinite polling loops hang when run non-interactively (no user to dismiss dialogs).
        code = Regex.Replace(code,
            @"Start-Process\s+""([^""]+)""\s+-ArgumentList\s+""/Uninstall""[\s\S]*?\}\s*else\s*\{[^}]*\}",
            match =>
            {
                var exePath = match.Groups[1].Value;
                var procName = Path.GetFileNameWithoutExtension(exePath);
                return $"# Non-interactive uninstall: {procName}\n" +
                       $"try {{ Start-Process \"{exePath}\" -ArgumentList '/Uninstall' -ErrorAction SilentlyContinue }} catch {{}}\n" +
                       $"Start-Sleep -Seconds 5\n" +
                       $"Stop-Process -Name {procName} -Force -ErrorAction SilentlyContinue";
            },
            RegexOptions.IgnoreCase);

        // Phase 2: Line-by-line cleanup
        var lines = code.Split('\n');
        var cleaned = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip interactive prompts and pauses
            if (trimmed.Contains("$Host.UI.RawUI.ReadKey")) continue;
            if (trimmed.StartsWith("$choice") && trimmed.Contains("Read-Host")) continue;
            if (Regex.IsMatch(trimmed, @"^Read-Host\b")) continue;

            // Skip settings URL openers (we handle post-script actions differently)
            if (trimmed.StartsWith("Start-Process ms-settings:")) continue;
            if (trimmed.StartsWith("Start-Process powercfg.cpl")) continue;

            // Skip show-menu calls
            if (trimmed == "show-menu") continue;

            // Keep exit only at end of block (actually skip it, we control flow)
            if (trimmed == "exit") continue;

            // Replace Timeout /T N (console command that hangs without stdin) with Start-Sleep
            var timeoutMatch = Regex.Match(trimmed, @"^Timeout\s+/T\s+(\d+)", RegexOptions.IgnoreCase);
            if (timeoutMatch.Success)
            {
                cleaned.Add($"Start-Sleep -Seconds {timeoutMatch.Groups[1].Value}");
                continue;
            }

            cleaned.Add(line);
        }

        return string.Join("\n", cleaned);
    }

    public static async Task<(bool Success, string Output)> ExecuteScriptAsync(
        OptimizationScript script, int choice, Action<string>? onOutput = null)
    {
        if (script.FileType == ScriptFileType.Cmd)
        {
            return await ExecuteCmdAsync(script.FullPath, onOutput);
        }

        if (script.FileType != ScriptFileType.PowerShell)
        {
            return (true, $"Skipped non-executable: {script.DisplayName}");
        }

        try
        {
            var content = await File.ReadAllTextAsync(script.FullPath);
            var codeBlock = ExtractChoiceBlock(content, choice);

            if (string.IsNullOrWhiteSpace(codeBlock))
            {
                return (false, $"Could not extract choice {choice} from {script.DisplayName}");
            }

            return await ExecutePowerShellBlockAsync(codeBlock, onOutput);
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    public static async Task<(bool Success, string Output)> ExecuteKritterScript(
        OptimizationScript script, bool optimizeMode, Action<string>? onOutput = null)
    {
        int choice;
        if (optimizeMode)
        {
            var fileName = Path.GetFileName(script.FullPath);
            choice = SpecialChoiceMap.GetValueOrDefault(fileName, 1);
        }
        else
        {
            // Default/keep-current mode
            var fileName = Path.GetFileName(script.FullPath);
            choice = SpecialChoiceMap.ContainsKey(fileName) ? 1 : 2; // Inverse for default
        }

        return await ExecuteScriptAsync(script, choice, onOutput);
    }

    private static async Task<(bool Success, string Output)> ExecutePowerShellBlockAsync(
        string codeBlock, Action<string>? onOutput = null)
    {
        var tempScript = Path.Combine(Path.GetTempPath(), $"kritter_{Guid.NewGuid():N}.ps1");
        try
        {
            var wrappedCode = $"$ErrorActionPreference = 'SilentlyContinue'\n$ProgressPreference = 'SilentlyContinue'\n{codeBlock}";
            await File.WriteAllTextAsync(tempScript, wrappedCode, Encoding.UTF8);

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
            var outputBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                    onOutput?.Invoke(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            var waitTask = process.WaitForExitAsync();
            var completedTask = await Task.WhenAny(waitTask, Task.Delay(ScriptExecutionTimeout));
            if (completedTask != waitTask)
            {
                TryKillProcess(process);
                outputBuilder.AppendLine($"Timeout: script {ScriptExecutionTimeout.TotalMinutes:0} dakikada tamamlanmadi.");
                return (false, outputBuilder.ToString());
            }

            await waitTask;

            return (process.ExitCode == 0, outputBuilder.ToString());
        }
        finally
        {
            try { File.Delete(tempScript); } catch { }
        }
    }

    private static async Task<(bool Success, string Output)> ExecuteCmdAsync(
        string cmdPath, Action<string>? onOutput = null)
    {
        try
        {
            // AllowScripts.cmd typically sets execution policy - we handle that ourselves
            var fileName = Path.GetFileName(cmdPath);
            if (fileName.Contains("AllowScripts", StringComparison.OrdinalIgnoreCase))
            {
                var result = await ExecutePowerShellBlockAsync(
                    "Set-ExecutionPolicy Bypass -Scope CurrentUser -Force", onOutput);
                return result;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{cmdPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            var waitTask = process.WaitForExitAsync();
            var completedTask = await Task.WhenAny(waitTask, Task.Delay(CommandExecutionTimeout));

            if (completedTask != waitTask)
            {
                TryKillProcess(process);
                var timedOutOutput = await outputTask;
                var timedOutError = await errorTask;
                var timeoutText = $"Timeout: komut {CommandExecutionTimeout.TotalMinutes:0} dakikada tamamlanmadi.";
                var mergedTimedOut = string.IsNullOrWhiteSpace(timedOutError)
                    ? $"{timeoutText}\n{timedOutOutput}"
                    : $"{timeoutText}\n{timedOutOutput}\n{timedOutError}";
                return (false, mergedTimedOut.Trim());
            }

            await waitTask;
            var output = await outputTask;
            var error = await errorTask;
            var merged = string.IsNullOrWhiteSpace(error) ? output : $"{output}\n{error}";

            return (process.ExitCode == 0, merged);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
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
