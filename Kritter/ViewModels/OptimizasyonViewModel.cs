using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Kritter.Localization;
using Kritter.Models;
using Kritter.Services;

namespace Kritter.ViewModels;

public class OptimizasyonViewModel : BaseViewModel
{
    private bool _isLoading;
    private bool _isRunning;
    private string _statusText = "";
    private string _currentStepText = "";
    private OptimizationMode _selectedMode = OptimizationMode.KritterRecommended;

    public ObservableCollection<OptimizationScript> KritterScripts { get; } = new();
    public ObservableCollection<OptimizationScript> SelectedFr33tyScripts { get; } = new();
    public ObservableCollection<string> LogEntries { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(CanApply)); }
    }

    public bool IsRunning
    {
        get => _isRunning;
        set { SetProperty(ref _isRunning, value); OnPropertyChanged(nameof(CanApply)); }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string CurrentStepText
    {
        get => _currentStepText;
        set => SetProperty(ref _currentStepText, value);
    }

    public string SelectedFr33tyScriptsSummary => AppText.Fr33tyScriptCountLabel(SelectedFr33tyScripts.Count);

    public OptimizationMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (SetProperty(ref _selectedMode, value))
            {
                OnPropertyChanged(nameof(IsKritterMode));
                OnPropertyChanged(nameof(IsFr33tyMode));
                OnPropertyChanged(nameof(CanApply));
                LoadScripts();
            }
        }
    }

    public bool IsKritterMode => _selectedMode == OptimizationMode.KritterRecommended;
    public bool IsFr33tyMode => _selectedMode == OptimizationMode.Fr33tyAll;
    public bool CanApply => !IsLoading && !IsRunning;

    public ICommand ApplyCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand ShowFr33tyModalCommand { get; }

    public OptimizasyonViewModel()
    {
        ApplyCommand = new RelayCommand(async () => await ApplyAsync(), () => CanApply);
        SelectAllCommand = new RelayCommand(() => { foreach (var s in KritterScripts) s.IsSelected = true; });
        DeselectAllCommand = new RelayCommand(() => { foreach (var s in KritterScripts) s.IsSelected = false; });
        ShowFr33tyModalCommand = new RelayCommand(ShowFr33tyModal);
        SelectedFr33tyScripts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(SelectedFr33tyScriptsSummary));

        LoadScripts();
    }

    private void LoadScripts()
    {
        KritterScripts.Clear();
        SelectedFr33tyScripts.Clear();

        if (_selectedMode == OptimizationMode.KritterRecommended)
        {
            IsLoading = true;
            try
            {
                var scripts = ScriptService.GetKritterRecommendedScripts();
                foreach (var s in scripts.Where(s => !s.Is00Item && s.FileType is ScriptFileType.PowerShell or ScriptFileType.Cmd))
                {
                    KritterScripts.Add(s);
                }

                StatusText = KritterScripts.Count > 0
                    ? AppText.KritterScriptsFound(KritterScripts.Count)
                    : AppText.KritterScriptsNotFound;
            }
            catch (Exception ex)
            {
                StatusText = AppText.ScriptLoadError(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
        else if (_selectedMode == OptimizationMode.Fr33tyAll)
        {
            StatusText = AppText.Fr33tySelectionPrompt;
        }
    }

    private void ShowFr33tyModal()
    {
        var vm = new Fr33tyModalViewModel();
        var modal = new Views.Fr33tyModal { DataContext = vm, Owner = Application.Current.MainWindow };

        if (modal.ShowDialog() == true)
        {
            var selected = vm.GetSelectedScripts();
            SelectedFr33tyScripts.Clear();
            foreach (var s in selected)
            {
                SelectedFr33tyScripts.Add(s);
            }

            StatusText = AppText.Fr33tyScriptCountLabel(SelectedFr33tyScripts.Count);
        }
    }

    private async Task ApplyAsync()
    {
        if (_selectedMode == OptimizationMode.KritterRecommended && KritterScripts.All(s => !s.IsSelected))
        {
            MessageBox.Show(AppText.SelectAtLeastOneScript, AppText.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_selectedMode == OptimizationMode.Fr33tyAll && SelectedFr33tyScripts.Count == 0)
        {
            MessageBox.Show(AppText.SelectFr33tyScripts, AppText.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsRunning = true;
        LogEntries.Clear();
        AddLog(AppText.OptimizationStarted);

        try
        {
            if (_selectedMode == OptimizationMode.KritterRecommended)
            {
                await ApplyKritterScriptsAsync();
            }
            else if (_selectedMode == OptimizationMode.Fr33tyAll)
            {
                await ApplyFr33tyScriptsAsync();
            }

            AddLog(string.Empty);
            AddLog(AppText.AllOptimizationsCompleted);
            StatusText = AppText.OptimizationsAppliedSuccessfully;
        }
        catch (Exception ex)
        {
            AddLog(AppText.ErrorPrefix(ex.Message));
            StatusText = AppText.OptimizationFailed;
        }
        finally
        {
            IsRunning = false;
            CurrentStepText = "";
        }
    }

    private async Task ApplyKritterScriptsAsync()
    {
        var selected = KritterScripts.Where(s => s.IsSelected).ToList();

        for (int i = 0; i < selected.Count; i++)
        {
            var script = selected[i];
            var pct = (int)((double)i / selected.Count * 100);
            var progressText = AppText.ApplyingProgress(script.DisplayName, pct);
            UpdateCurrentStep(progressText);
            AddLiveLog(progressText);

            var (success, _) = await ScriptService.ExecuteKritterScript(script, optimizeMode: true);

            ReplaceLiveLog(success
                ? AppText.Applied(script.DisplayName)
                : AppText.ApplyFailedContinue(script.DisplayName));

            if (i < selected.Count - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }

    private async Task ApplyFr33tyScriptsAsync()
    {
        var scriptsRoot = Path.Combine(AppContext.BaseDirectory, "Scripts", "fr33ty Recommended");

        for (int i = 0; i < SelectedFr33tyScripts.Count; i++)
        {
            var ps = SelectedFr33tyScripts[i];
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

            var pct = (int)((double)i / SelectedFr33tyScripts.Count * 100);
            var progressText = AppText.ApplyingProgress(script.DisplayName, pct);
            UpdateCurrentStep(progressText);
            AddLiveLog(progressText);

            var (success, _) = await ScriptService.ExecuteScriptAsync(script, 1);

            ReplaceLiveLog(success
                ? AppText.Applied(script.DisplayName)
                : AppText.ApplyFailedContinue(script.DisplayName));

            if (i < SelectedFr33tyScripts.Count - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
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
}
