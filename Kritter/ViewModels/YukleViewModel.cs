using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
        set { SetProperty(ref _loadedPackage, value); OnPropertyChanged(nameof(HasPackage)); }
    }

    public bool HasPackage => _loadedPackage != null;

    public string PackageInfo
    {
        get => _packageInfo;
        set => SetProperty(ref _packageInfo, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
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
            Filter = "Kritter Paket (*.kritter)|*.kritter",
            Title = "Kritter Paketi Seç"
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
                MessageBox.Show("Paket okunamadı.", "Kritter", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadedPackage = pkg;
            _packageFilePath = path;

            string modeName = pkg.OptimizationMode switch
            {
                OptimizationMode.KritterRecommended => "Kritter Önerilen",
                OptimizationMode.Fr33tyAll => "Fr33ty Tüm Optimizasyon",
                OptimizationMode.KeepCurrent => "Ayarları Koru",
                _ => "?"
            };

            var wingetCount = pkg.Apps.Count(a => a.InstallMethod == InstallMethod.Winget);
            var directCount = pkg.Apps.Count(a => a.InstallMethod == InstallMethod.Direct);
            var setupCount = pkg.SetupInstallers.Count;

            PackageInfo =
                $"Mod: {modeName}\n" +
                $"Uygulama: {pkg.Apps.Count} adet (Winget: {wingetCount}, Direct: {directCount})\n" +
                $"Setup: {setupCount} adet\n" +
                $"Oluşturma: {pkg.CreatedAt:g}";

            if (pkg.OptimizationMode == OptimizationMode.Fr33tyAll && pkg.Fr33tyScripts.Count > 0)
            {
                PackageInfo += $"\nFr33ty Script: {pkg.Fr33tyScripts.Count} adet";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Paket yükleme hatası: {ex.Message}", "Kritter", MessageBoxButton.OK, MessageBoxImage.Error);
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

        AddLog("Yeniden başlatma sonrası devam ediliyor...");

        IsRunning = true;
        try
        {
            await InstallAppsPhase();
            await InstallSetupFilesPhase();
            await CleanupPhase();
            AddLog("");
            AddLog("Tüm işlemler tamamlandı.");
        }
        catch (Exception ex)
        {
            AddLog($"HATA: {ex.Message}");
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
                AddLog("Optimizasyon atlandı (ayarlar korunuyor).");
            }

            if (needsReboot)
            {
                var logSnapshot = new List<string>(LogEntries);
                ResumeService.SetupAutoResume(_packageFilePath!, logSnapshot);

                AddLog("");
                AddLog("Optimizasyonlar tamamlandı. Bilgisayarınızı yeniden başlatın.");
                CurrentStepText = "Yeniden başlatma bekleniyor...";
                IsRunning = false;
                IsWaitingForRestart = true;
                return;
            }

            await InstallAppsPhase();
            await InstallSetupFilesPhase();
            await CleanupPhase();

            AddLog("");
            AddLog("Tüm işlemler tamamlandı.");
        }
        catch (Exception ex)
        {
            AddLog($"HATA: {ex.Message}");
        }
        finally
        {
            IsRunning = false;
            if (!IsWaitingForRestart)
                CurrentStepText = "";
        }
    }

    private void ExecuteRestart()
    {
        Process.Start("shutdown", "/r /t 5 /c \"Kritter optimizasyonları uygulandı. Yeniden başlatılıyor...\"");
        Application.Current.Shutdown();
    }

    private async Task<bool> OptimizationPhase()
    {
        AddLog("--- Optimizasyon Aşaması ---");
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
                var progressText = $"{script.DisplayName} uygulanıyor... %{(int)((double)i / executableScripts.Count * 100)}";
                UpdateCurrentStep(progressText);
                AddLiveLog(progressText);

                var (success, _) = await ScriptService.ExecuteKritterScript(script, optimizeMode: true);
                anyApplied = true;

                ReplaceLiveLog(success
                    ? $"{script.DisplayName} uygulandı."
                    : $"{script.DisplayName} - uygulanamadı (devam ediliyor).");

                await DelayBetweenStepsAsync(i, executableScripts.Count);
            }
        }
        else if (LoadedPackage.OptimizationMode == OptimizationMode.Fr33tyAll)
        {
            var packageScripts = LoadedPackage.Fr33tyScripts;
            if (packageScripts.Count == 0)
            {
                AddLog("Fr33ty script seçilmemiş.");
                return false;
            }

            var scriptsRoot = Path.Combine(AppContext.BaseDirectory, "Scripts", "fr33ty Recommended");
            for (int i = 0; i < packageScripts.Count; i++)
            {
                var ps = packageScripts[i];
                var fullPath = Path.Combine(scriptsRoot, ps.RelativePath);

                if (!File.Exists(fullPath))
                {
                    AddLog($"{ps.DisplayName} - dosya bulunamadı, atlanıyor.");
                    continue;
                }

                var script = new OptimizationScript
                {
                    FullPath = fullPath,
                    RelativePath = ps.RelativePath,
                    DisplayName = ps.DisplayName,
                    FileType = Path.GetExtension(fullPath).ToLowerInvariant() == ".ps1"
                        ? ScriptFileType.PowerShell : ScriptFileType.Cmd
                };

                var progressText = $"{script.DisplayName} uygulanıyor... %{(int)((double)i / packageScripts.Count * 100)}";
                UpdateCurrentStep(progressText);
                AddLiveLog(progressText);

                var (success, _) = await ScriptService.ExecuteScriptAsync(script, 1);
                anyApplied = true;

                ReplaceLiveLog(success
                    ? $"{script.DisplayName} uygulandı."
                    : $"{script.DisplayName} - uygulanamadı (devam ediliyor).");

                await DelayBetweenStepsAsync(i, packageScripts.Count);
            }
        }

        if (reminderItems.Count > 0)
        {
            AddLog("");
            AddLog("--- Hatırlatmalar ---");
            foreach (var item in reminderItems)
            {
                AddLog($"  Manuel ayar gerekli: {item.DisplayName}");
            }
        }

        return anyApplied;
    }

    private async Task InstallAppsPhase()
    {
        if (LoadedPackage?.Apps == null || LoadedPackage.Apps.Count == 0)
        {
            AddLog("Yüklenecek uygulama yok.");
            return;
        }

        AddLog("");
        AddLog("--- Uygulama Yükleme Aşaması ---");

        for (int i = 0; i < LoadedPackage.Apps.Count; i++)
        {
            var app = LoadedPackage.Apps[i];
            var pct = (int)((double)i / LoadedPackage.Apps.Count * 100);
            var progressText = $"{app.Name} yükleniyor... %{pct}";
            UpdateCurrentStep(progressText);
            AddLiveLog(progressText);

            var (success, output) = await WingetService.InstallAppAsync(app);

            if (success)
            {
                if (output.Contains("already installed", StringComparison.OrdinalIgnoreCase))
                {
                    ReplaceLiveLog($"{app.Name} ({app.MethodLabel}) zaten yüklü.");
                }
                else
                {
                    ReplaceLiveLog($"{app.Name} ({app.MethodLabel}) yüklendi.");
                }
            }
            else
            {
                ReplaceLiveLog($"{app.Name} ({app.MethodLabel}) - yüklenemedi.");
            }

            await DelayBetweenStepsAsync(i, LoadedPackage.Apps.Count);
        }
    }

    private async Task InstallSetupFilesPhase()
    {
        if (LoadedPackage?.SetupInstallers == null || LoadedPackage.SetupInstallers.Count == 0)
        {
            AddLog("Setup kurulumu gerekmiyor.");
            return;
        }

        AddLog("");
        AddLog("--- Setup Dosyaları Kurulum Aşaması ---");

        for (int i = 0; i < LoadedPackage.SetupInstallers.Count; i++)
        {
            var installer = LoadedPackage.SetupInstallers[i];
            var pct = (int)((double)i / LoadedPackage.SetupInstallers.Count * 100);
            var progressText = $"{installer.DisplayName} (Setup) kuruluyor... %{pct}";
            UpdateCurrentStep(progressText);
            AddLiveLog(progressText);

            var (success, output) = await SetupInstallerService.InstallAsync(installer);
            if (success)
            {
                ReplaceLiveLog($"{installer.DisplayName} (Setup) kuruldu.");
            }
            else
            {
                ReplaceLiveLog($"{installer.DisplayName} (Setup) - kurulamadı.");
                if (!string.IsNullOrWhiteSpace(output))
                {
                    AddLog(output.Trim());
                }
            }

            await DelayBetweenStepsAsync(i, LoadedPackage.SetupInstallers.Count);
        }
    }

    private async Task CleanupPhase()
    {
        AddLog("");
        AddLog("--- Temizlik Aşaması ---");
        UpdateCurrentStep("Geçici dosyalar temizleniyor...");
        AddLiveLog("Geçici dosyalar temizleniyor...");

        await CleanupService.RunCleanupAsync();

        ReplaceLiveLog("Geçici dosyalar temizlendi.");
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
