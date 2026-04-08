using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Kritter.Localization;
using Kritter.Models;
using Kritter.Services;
using Microsoft.Win32;

namespace Kritter.ViewModels;

public class YukleViewModel : BaseViewModel
{
    private static readonly TimeSpan StepDelay = TimeSpan.FromSeconds(3);

    private KritterPackage? _loadedPackage;
    private string _packageInfo = "";
    private bool _isRunning;
    private bool _isWaitingForRestart;
    private string _currentStepText = "";

    public ObservableCollection<string> LogEntries { get; } = new();

    public KritterPackage? LoadedPackage
    {
        get => _loadedPackage;
        set
        {
            SetProperty(ref _loadedPackage, value);
            OnPropertyChanged(nameof(HasPackage));
            OnPropertyChanged(nameof(PackageStatusSummary));
        }
    }

    public bool HasPackage => _loadedPackage != null;
    public string PackageStatusSummary => LoadedPackage != null ? AppText.ReadyLabel : AppText.NoPackageLabel;
    public string ExecutionStateSummary => IsRunning ? AppText.RunningLabel : AppText.IdleLabel;

    public string PackageInfo
    {
        get => _packageInfo;
        set => SetProperty(ref _packageInfo, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(ExecutionStateSummary));
            }
        }
    }

    public bool IsWaitingForRestart
    {
        get => _isWaitingForRestart;
        set => SetProperty(ref _isWaitingForRestart, value);
    }

    public string CurrentStepText
    {
        get => _currentStepText;
        set => SetProperty(ref _currentStepText, value);
    }

    public ICommand LoadPackageCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand RestartCommand { get; }

    private string? _packageFilePath;

    public YukleViewModel()
    {
        LoadPackageCommand = new RelayCommand(async () => await LoadPackageAsync(), () => !IsRunning && !IsWaitingForRestart);
        StartCommand = new RelayCommand(async () => await StartExecutionAsync(), () => HasPackage && !IsRunning && !IsWaitingForRestart);
        RestartCommand = new RelayCommand(ExecuteRestart, () => IsWaitingForRestart);
    }

    private async Task LoadPackageAsync()
    {
        var ofd = new OpenFileDialog
        {
            Filter = AppText.BuildPackageFilter,
            Title = AppText.SelectPackageTitle
        };

        if (ofd.ShowDialog() == true)
        {
            await LoadFromPath(ofd.FileName);
        }
    }

    private async Task LoadFromPath(string path)
    {
        try
        {
            var pkg = await PackageService.LoadPackageAsync(path);
            if (pkg == null)
            {
                MessageBox.Show(AppText.PackageReadFailed, AppText.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadedPackage = pkg;
            _packageFilePath = path;

            string modeName = pkg.OptimizationMode switch
            {
                OptimizationMode.KritterRecommended => AppText.KritterRecommended,
                OptimizationMode.Fr33tyAll => AppText.Fr33tyAll,
                OptimizationMode.KeepCurrent => AppText.KeepCurrent,
                _ => "?"
            };

            var wingetCount = pkg.Apps.Count(a => a.InstallMethod == InstallMethod.Winget);
            var directCount = pkg.Apps.Count(a => a.InstallMethod == InstallMethod.Direct);
            var setupCount = pkg.SetupInstallers.Count;
            var gameSettingsCount = pkg.GameSettingsBackups.Count;

            PackageInfo =
                $"{AppText.ModeLabel}: {modeName}\n" +
                $"{AppText.AppsLabel}: {pkg.Apps.Count} {AppText.ItemsSuffix} ({AppText.WingetLabel}: {wingetCount}, {AppText.DirectLabel}: {directCount})\n" +
                $"{AppText.SetupLabel}: {setupCount} {AppText.ItemsSuffix}\n" +
                $"{AppText.GameSettingsLabel}: {gameSettingsCount} {AppText.ItemsSuffix}\n" +
                $"{AppText.CreatedAtLabel}: {pkg.CreatedAt:g}";

            if (pkg.OptimizationMode == OptimizationMode.Fr33tyAll && pkg.Fr33tyScripts.Count > 0)
            {
                PackageInfo += $"\n{AppText.Fr33tyScriptLabel}: {pkg.Fr33tyScripts.Count} {AppText.ItemsSuffix}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(AppText.PackageLoadError(ex.Message), AppText.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async void HandleResume(string packagePath)
    {
        var resumeInfo = ResumeService.GetResumeInfo();
        ResumeService.ClearResume();

        await LoadFromPath(packagePath);

        if (LoadedPackage == null)
        {
            return;
        }

        if (resumeInfo?.LogHistory != null)
        {
            foreach (var entry in resumeInfo.LogHistory)
            {
                LogEntries.Add(entry);
            }
        }

        AddLog(AppText.ResumeAfterRestart);

        IsRunning = true;
        try
        {
            await InstallAppsPhase();
            await RestoreGameSettingsPhase();
            await InstallSetupFilesPhase();
            await CleanupPhase();
            AddLog(string.Empty);
            AddLog(AppText.AllTasksCompleted);
        }
        catch (Exception ex)
        {
            AddLog(AppText.ErrorPrefix(ex.Message));
        }
        finally
        {
            IsRunning = false;
            CurrentStepText = "";
        }
    }

    private async Task StartExecutionAsync()
    {
        if (LoadedPackage == null)
        {
            return;
        }

        IsRunning = true;
        LogEntries.Clear();

        try
        {
            bool needsReboot = false;

            if (LoadedPackage.OptimizationMode != OptimizationMode.KeepCurrent)
            {
                needsReboot = await OptimizationPhase();
            }
            else
            {
                AddLog(AppText.OptimizationSkipped);
            }

            if (needsReboot)
            {
                var logSnapshot = new List<string>(LogEntries);
                ResumeService.SetupAutoResume(_packageFilePath!, logSnapshot);

                AddLog(string.Empty);
                AddLog(AppText.OptimizationsCompletedRestart);
                CurrentStepText = AppText.WaitingForRestart;
                IsRunning = false;
                IsWaitingForRestart = true;
                return;
            }

            await InstallAppsPhase();
            await RestoreGameSettingsPhase();
            await InstallSetupFilesPhase();
            await CleanupPhase();

            AddLog(string.Empty);
            AddLog(AppText.AllTasksCompleted);
        }
        catch (Exception ex)
        {
            AddLog(AppText.ErrorPrefix(ex.Message));
        }
        finally
        {
            IsRunning = false;
            if (!IsWaitingForRestart)
            {
                CurrentStepText = "";
            }
        }
    }

    private void ExecuteRestart()
    {
        Process.Start("shutdown", $"/r /t 5 /c \"{AppText.RestartShutdownComment}\"");
        Application.Current.Shutdown();
    }

    private async Task<bool> OptimizationPhase()
    {
        AddLog(AppText.OptimizationPhase);
        bool anyApplied = false;
        var reminderItems = new List<OptimizationScript>();

        if (LoadedPackage!.OptimizationMode == OptimizationMode.KritterRecommended)
        {
            var scripts = ScriptService.GetKritterRecommendedScripts();
            var executableScripts = scripts.Where(s => !s.Is00Item && s.FileType is ScriptFileType.PowerShell or ScriptFileType.Cmd).ToList();
            reminderItems = scripts.Where(s => s.Is00Item).ToList();

            for (int i = 0; i < executableScripts.Count; i++)
            {
                var script = executableScripts[i];
                var progressText = AppText.ApplyingProgress(script.DisplayName, (int)((double)i / executableScripts.Count * 100));
                UpdateCurrentStep(progressText);
                AddLiveLog(progressText);

                var (success, _) = await ScriptService.ExecuteKritterScript(script, optimizeMode: true);
                anyApplied = true;

                ReplaceLiveLog(success
                    ? AppText.Applied(script.DisplayName)
                    : AppText.ApplyFailedContinue(script.DisplayName));

                await DelayBetweenStepsAsync(i, executableScripts.Count);
            }
        }
        else if (LoadedPackage.OptimizationMode == OptimizationMode.Fr33tyAll)
        {
            var packageScripts = LoadedPackage.Fr33tyScripts;
            if (packageScripts.Count == 0)
            {
                AddLog(AppText.NoFr33tyScriptsSelected);
                return false;
            }

            var scriptsRoot = Path.Combine(AppContext.BaseDirectory, "Scripts", "fr33ty Recommended");
            for (int i = 0; i < packageScripts.Count; i++)
            {
                var ps = packageScripts[i];
                var fullPath = Path.Combine(scriptsRoot, ps.RelativePath);

                if (!File.Exists(fullPath))
                {
                    AddLog(AppText.FileMissingSkipped(ps.DisplayName));
                    continue;
                }

                var script = new OptimizationScript
                {
                    FullPath = fullPath,
                    RelativePath = ps.RelativePath,
                    DisplayName = ps.DisplayName,
                    FileType = Path.GetExtension(fullPath).ToLowerInvariant() == ".ps1"
                        ? ScriptFileType.PowerShell
                        : ScriptFileType.Cmd
                };

                var progressText = AppText.ApplyingProgress(script.DisplayName, (int)((double)i / packageScripts.Count * 100));
                UpdateCurrentStep(progressText);
                AddLiveLog(progressText);

                var (success, _) = await ScriptService.ExecuteScriptAsync(script, 1);
                anyApplied = true;

                ReplaceLiveLog(success
                    ? AppText.Applied(script.DisplayName)
                    : AppText.ApplyFailedContinue(script.DisplayName));

                await DelayBetweenStepsAsync(i, packageScripts.Count);
            }
        }

        if (reminderItems.Count > 0)
        {
            AddLog(string.Empty);
            AddLog(AppText.RemindersHeader);
            foreach (var item in reminderItems)
            {
                AddLog(AppText.ManualSettingRequired(item.DisplayName));
            }
        }

        return anyApplied;
    }

    private async Task InstallAppsPhase()
    {
        if (LoadedPackage?.Apps == null || LoadedPackage.Apps.Count == 0)
        {
            AddLog(AppText.NoAppsToInstall);
            return;
        }

        AddLog(string.Empty);
        AddLog(AppText.AppInstallPhase);

        for (int i = 0; i < LoadedPackage.Apps.Count; i++)
        {
            var app = LoadedPackage.Apps[i];
            var progressText = AppText.InstallingProgress(app.Name, (int)((double)i / LoadedPackage.Apps.Count * 100));
            UpdateCurrentStep(progressText);
            AddLiveLog(progressText);

            var (success, output) = await WingetService.InstallAppAsync(app);

            if (success)
            {
                ReplaceLiveLog(output.Contains("already installed", StringComparison.OrdinalIgnoreCase)
                    ? AppText.AlreadyInstalled(app.Name, app.MethodLabel)
                    : AppText.Installed(app.Name, app.MethodLabel));
            }
            else
            {
                ReplaceLiveLog(AppText.InstallFailed(app.Name, app.MethodLabel));
            }

            await DelayBetweenStepsAsync(i, LoadedPackage.Apps.Count);
        }
    }

    private async Task InstallSetupFilesPhase()
    {
        if (LoadedPackage?.SetupInstallers == null || LoadedPackage.SetupInstallers.Count == 0)
        {
            AddLog(AppText.NoSetupInstallNeeded);
            return;
        }

        AddLog(string.Empty);
        AddLog(AppText.SetupInstallPhase);

        for (int i = 0; i < LoadedPackage.SetupInstallers.Count; i++)
        {
            var installer = LoadedPackage.SetupInstallers[i];
            var progressText = AppText.SetupInstallingProgress(installer.DisplayName, (int)((double)i / LoadedPackage.SetupInstallers.Count * 100));
            UpdateCurrentStep(progressText);
            AddLiveLog(progressText);

            var (success, output) = await SetupInstallerService.InstallAsync(installer);
            if (success)
            {
                ReplaceLiveLog(AppText.SetupInstalled(installer.DisplayName));
            }
            else
            {
                ReplaceLiveLog(AppText.SetupInstallFailed(installer.DisplayName));
                if (!string.IsNullOrWhiteSpace(output))
                {
                    AddLog(output.Trim());
                }
            }

            await DelayBetweenStepsAsync(i, LoadedPackage.SetupInstallers.Count);
        }
    }

    private async Task RestoreGameSettingsPhase()
    {
        if (LoadedPackage?.GameSettingsBackups == null || LoadedPackage.GameSettingsBackups.Count == 0)
        {
            AddLog(AppText.NoGameSettingsRestoreNeeded);
            return;
        }

        AddLog(string.Empty);
        AddLog(AppText.GameSettingsRestorePhase);

        for (int i = 0; i < LoadedPackage.GameSettingsBackups.Count; i++)
        {
            var backup = LoadedPackage.GameSettingsBackups[i];
            var progressText = AppText.RestoringProgress(backup.DisplayName, (int)((double)i / LoadedPackage.GameSettingsBackups.Count * 100));
            UpdateCurrentStep(progressText);
            AddLiveLog(progressText);

            var (success, output) = await GameSettingsService.RestoreAsync(backup);
            if (success)
            {
                ReplaceLiveLog(AppText.Restored(backup.DisplayName));
            }
            else
            {
                ReplaceLiveLog(AppText.RestoreFailed(backup.DisplayName));
                if (!string.IsNullOrWhiteSpace(output))
                {
                    AddLog(output.Trim());
                }
            }

            await DelayBetweenStepsAsync(i, LoadedPackage.GameSettingsBackups.Count);
        }
    }

    private async Task CleanupPhase()
    {
        AddLog(string.Empty);
        AddLog(AppText.CleanupPhase);
        UpdateCurrentStep(AppText.CleaningTemporaryFiles);
        AddLiveLog(AppText.CleaningTemporaryFiles);

        await CleanupService.RunCleanupAsync();

        ReplaceLiveLog(AppText.TemporaryFilesCleaned);
        CurrentStepText = "";
    }

    private int _liveLogIndex = -1;

    private void AddLog(string text)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogEntries.Add(text);
            _liveLogIndex = -1;
        });
    }

    private void AddLiveLog(string text)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogEntries.Add(text);
            _liveLogIndex = LogEntries.Count - 1;
        });
    }

    private void ReplaceLiveLog(string text)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_liveLogIndex >= 0 && _liveLogIndex < LogEntries.Count)
            {
                LogEntries[_liveLogIndex] = text;
            }

            _liveLogIndex = -1;
        });
    }

    private void UpdateCurrentStep(string text)
    {
        Application.Current.Dispatcher.Invoke(() => CurrentStepText = text);
    }

    private static async Task DelayBetweenStepsAsync(int currentIndex, int totalCount)
    {
        if (currentIndex < totalCount - 1)
        {
            await Task.Delay(StepDelay);
        }
    }
}
