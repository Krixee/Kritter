using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Kritter.Models;
using Kritter.Services;

namespace Kritter.ViewModels;

public class ScriptCategoryViewModel : BaseViewModel
{
    private bool _isExpanded;

    public string CategoryName { get; set; } = "";
    public ObservableCollection<OptimizationScript> Scripts { get; set; } = new();

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public int SelectedCount => Scripts.Count(s => s.IsSelected);
    public int TotalCount => Scripts.Count;

    public void SelectAll() { foreach (var s in Scripts) s.IsSelected = true; OnPropertyChanged(nameof(SelectedCount)); }
    public void DeselectAll() { foreach (var s in Scripts) s.IsSelected = false; OnPropertyChanged(nameof(SelectedCount)); }

    public void RefreshCount() => OnPropertyChanged(nameof(SelectedCount));
}

public class Fr33tyModalViewModel : BaseViewModel
{
    public ObservableCollection<ScriptCategoryViewModel> Categories { get; } = new();

    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }

    public Fr33tyModalViewModel()
    {
        SelectAllCommand = new RelayCommand(() =>
        {
            foreach (var cat in Categories)
                cat.SelectAll();
        });

        DeselectAllCommand = new RelayCommand(() =>
        {
            foreach (var cat in Categories)
                cat.DeselectAll();
        });

        LoadScripts();
    }

    private void LoadScripts()
    {
        var categorized = ScriptService.GetFr33tyScripts();

        foreach (var (category, scripts) in categorized)
        {
            var catVm = new ScriptCategoryViewModel
            {
                CategoryName = category
            };

            // Only include executable scripts (PS1, CMD), not URL/LNK
            foreach (var s in scripts.Where(s => s.FileType is ScriptFileType.PowerShell or ScriptFileType.Cmd))
            {
                catVm.Scripts.Add(s);
            }

            if (catVm.Scripts.Count > 0)
            {
                Categories.Add(catVm);
            }
        }
    }

    public List<OptimizationScript> GetSelectedScripts()
    {
        return Categories
            .SelectMany(c => c.Scripts)
            .Where(s => s.IsSelected)
            .ToList();
    }
}
