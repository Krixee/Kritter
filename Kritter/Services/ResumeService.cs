using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using Kritter.Models;

namespace Kritter.Services;

public static class ResumeService
{
    private static readonly string ResumeDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kritter");
    private static readonly string ResumeFile = Path.Combine(ResumeDir, "resume.json");
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "Kritter";

    public static void SetupAutoResume(string packagePath, List<string> logHistory)
    {
        Directory.CreateDirectory(ResumeDir);

        var info = new ResumeInfo
        {
            PackagePath = packagePath,
            Phase = "install_apps",
            LogHistory = logHistory
        };

        var json = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ResumeFile, json);

        var exePath = Environment.ProcessPath ?? "";
        if (!string.IsNullOrEmpty(exePath))
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            key?.SetValue(RunValueName, $"\"{exePath}\" --resume");
        }
    }

    public static ResumeInfo? GetResumeInfo()
    {
        if (!File.Exists(ResumeFile)) return null;

        try
        {
            var json = File.ReadAllText(ResumeFile);
            return JsonSerializer.Deserialize<ResumeInfo>(json);
        }
        catch
        {
            return null;
        }
    }

    public static void ClearResume()
    {
        try
        {
            if (File.Exists(ResumeFile))
                File.Delete(ResumeFile);

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            key?.DeleteValue(RunValueName, false);
        }
        catch
        {
            // Best effort cleanup
        }
    }
}
