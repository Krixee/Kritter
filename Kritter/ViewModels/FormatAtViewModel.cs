using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Kritter.Models;
using Kritter.Services;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace Kritter.ViewModels;

public class FormatAtViewModel : BaseViewModel
{
    private bool _isScanning;
    private bool _scanComplete;
    private OptimizationMode _selectedMode = OptimizationMode.KritterRecommended;
    private string _statusText = "";
    private string _setupFolderPath = "";

    public ObservableCollection<WingetApp> InstalledApps { get; } = new();
    public ObservableCollection<WingetApp> CommonApps { get; } = new();
    public ObservableCollection<SetupInstaller> SetupInstallers { get; } = new();
    public ObservableCollection<GameSettingsBackup> GameSettingsBackups { get; } = new();

    public bool IsScanning
    {
        get => _isScanning;
        set { SetProperty(ref _isScanning, value); OnPropertyChanged(nameof(ShowScanButton)); }
    }

    public bool ScanComplete
    {
        get => _scanComplete;
        set { SetProperty(ref _scanComplete, value); OnPropertyChanged(nameof(ShowScanButton)); }
    }

    public bool ShowScanButton => !IsScanning && !ScanComplete;

    public OptimizationMode SelectedMode
    {
        get => _selectedMode;
        set { SetProperty(ref _selectedMode, value); OnPropertyChanged(nameof(IsFr33tyMode)); }
    }

    public bool IsFr33tyMode => _selectedMode == OptimizationMode.Fr33tyAll;

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string SetupFolderPath
    {
        get => _setupFolderPath;
        set => SetProperty(ref _setupFolderPath, value);
    }

    public bool HasSetupInstallers => SetupInstallers.Count > 0;
    public bool HasGameSettingsBackups => GameSettingsBackups.Count > 0;

    public ICommand ScanCommand { get; }
    public ICommand CreatePackageCommand { get; }
    public ICommand SelectAllInstalledCommand { get; }
    public ICommand DeselectAllInstalledCommand { get; }
    public ICommand ShowFr33tyModalCommand { get; }
    public ICommand SelectSetupFolderCommand { get; }
    public ICommand SelectCs2AccountCommand { get; }

    public FormatAtViewModel()
    {
        ScanCommand = new RelayCommand(async () => await ScanAsync(), () => !IsScanning);
        CreatePackageCommand = new RelayCommand(async () => await CreatePackageAsync(), () => ScanComplete && !IsScanning);
        SelectAllInstalledCommand = new RelayCommand(() => { foreach (var a in InstalledApps) a.IsSelected = true; });
        DeselectAllInstalledCommand = new RelayCommand(() => { foreach (var a in InstalledApps) a.IsSelected = false; });
        ShowFr33tyModalCommand = new RelayCommand(ShowFr33tyModal);
        SelectSetupFolderCommand = new RelayCommand(async () => await SelectSetupFolderAsync(), () => !IsScanning);
        SelectCs2AccountCommand = new RelayCommand(async () => await SelectCs2AccountAsync(), () => !IsScanning);

        SetupInstallers.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSetupInstallers));
        GameSettingsBackups.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasGameSettingsBackups));

        Task.Run(async () =>
        {
            await Task.Delay(500);
            await Application.Current.Dispatcher.InvokeAsync(async () => await ScanAsync());
        });
    }

    private async Task ScanAsync()
    {
        IsScanning = true;
        StatusText = "Sistem taranıyor...";
        InstalledApps.Clear();
        CommonApps.Clear();

        try
        {
            var installed = await WingetService.ScanInstalledAppsAsync();

            foreach (var app in installed.OrderBy(a => a.Name))
            {
                InstalledApps.Add(app);
            }

            var common = WingetService.GetCommonApps(installed);
            foreach (var app in common)
            {
                CommonApps.Add(app);
            }

            StatusText = InstalledApps.Count == 0
                ? "Yeniden kurulabilir uygulama bulunamadı."
                : $"{InstalledApps.Count} yeniden kurulabilir uygulama tespit edildi";
            ScanComplete = true;
        }
        catch (Exception ex)
        {
            StatusText = $"Tarama hatası: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private async Task SelectSetupFolderAsync()
    {
        MessageBox.Show(
            "Driver'ları manuel kurmanız önerilir. Lütfen seçtiğiniz klasörde driver kurulum dosyaları varsa çıkarın.",
            "Setup Klasörü Uyarısı",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        using var dialog = new WinForms.FolderBrowserDialog
        {
            Description = "Setup dosyalarının olduğu klasörü seçin.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() != WinForms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            return;
        }

        SetupFolderPath = dialog.SelectedPath;
        var detected = await Task.Run(() => SetupScannerService.ScanFolder(SetupFolderPath));

        SetupInstallers.Clear();
        foreach (var installer in detected)
        {
            SetupInstallers.Add(installer);
        }

        StatusText = detected.Count == 0
            ? "Seçilen klasörde uygun setup dosyası bulunamadı."
            : $"{detected.Count} setup dosyası bulundu ve pakete eklenebilir.";
    }

    private async Task CreatePackageAsync()
    {
        var selectedApps = InstalledApps.Where(a => a.IsSelected)
            .Concat(CommonApps.Where(a => a.IsSelected))
            .ToList();

        var selectedSetupInstallers = SetupInstallers
            .Where(s => s.IsSelected)
            .Select(s => s.CloneForPackage())
            .ToList();

        var selectedGameSettings = GameSettingsBackups
            .Where(g => g.IsSelected)
            .Select(g => g.CloneForPackage())
            .ToList();

        if (selectedApps.Count == 0 && selectedSetupInstallers.Count == 0 && selectedGameSettings.Count == 0)
        {
            MessageBox.Show("Lütfen en az bir uygulama, setup dosyası veya oyun ayarı seçin.", "Kritter", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var package = new KritterPackage
        {
            OptimizationMode = SelectedMode,
            Apps = selectedApps.Select(a => new WingetApp
            {
                Id = a.Id,
                Name = a.Name,
                InstallMethod = a.InstallMethod,
                DirectInstallerUrl = a.DirectInstallerUrl,
                DirectInstallerArgs = a.DirectInstallerArgs
            }).ToList(),
            SetupInstallers = selectedSetupInstallers,
            GameSettingsBackups = selectedGameSettings
        };

        if (SelectedMode == OptimizationMode.Fr33tyAll && _selectedFr33tyScripts != null)
        {
            package.Fr33tyScripts = _selectedFr33tyScripts
                .Select(s => new OptimizationScript { RelativePath = s.RelativePath, DisplayName = s.DisplayName })
                .ToList();
        }

        var defaultPath = PackageService.GetDefaultSavePath();

        var sfd = new SaveFileDialog
        {
            Filter = "Kritter Paket (*.kritter)|*.kritter",
            FileName = System.IO.Path.GetFileName(defaultPath),
            InitialDirectory = System.IO.Path.GetDirectoryName(defaultPath) ?? ""
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                await PackageService.SavePackageAsync(sfd.FileName, package);
                StatusText = $"Paket oluşturuldu: {System.IO.Path.GetFileName(sfd.FileName)}";
                MessageBox.Show($"Paket başarıyla oluşturuldu!\n\n{sfd.FileName}", "Kritter",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Paket oluşturma hatası: {ex.Message}", "Kritter",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private System.Collections.Generic.List<OptimizationScript>? _selectedFr33tyScripts;

    private async Task SelectCs2AccountAsync()
    {
        StatusText = "Steam/CS2 hesapları aranıyor...";

        try
        {
            var accounts = await GameSettingsService.DiscoverCs2AccountsAsync();
            if (accounts.Count == 0)
            {
                MessageBox.Show(
                    "Steam `userdata` içinde 730 klasörü bulunan bir hesap bulunamadı.",
                    "Kritter",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                StatusText = "CS2 hesabı bulunamadı.";
                return;
            }

            var vm = new SteamAccountSelectionViewModel(accounts);
            var modal = new Views.SteamAccountSelectionWindow
            {
                DataContext = vm,
                Owner = Application.Current.MainWindow
            };

            if (modal.ShowDialog() != true || vm.SelectedAccount == null)
            {
                StatusText = "CS2 hesap seçimi iptal edildi.";
                return;
            }

            GameSettingsBackups.Clear();
            GameSettingsBackups.Add(GameSettingsService.CreateCs2Backup(vm.SelectedAccount));

            StatusText = $"CS2 hesabı seçildi: {vm.SelectedAccount.DisplayName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"CS2 hesapları okunamadı: {ex.Message}", "Kritter", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText = "CS2 hesap taraması başarısız oldu.";
        }
    }

    private void ShowFr33tyModal()
    {
        var vm = new Fr33tyModalViewModel();
        var modal = new Views.Fr33tyModal { DataContext = vm, Owner = Application.Current.MainWindow };

        if (modal.ShowDialog() == true)
        {
            _selectedFr33tyScripts = vm.GetSelectedScripts();
            StatusText = $"{_selectedFr33tyScripts.Count} Fr33ty script seçildi";
        }
    }
}
