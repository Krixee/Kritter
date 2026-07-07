using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Kritter.Localization;
using Kritter.Services;

namespace Kritter.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel _currentPage;
    private int _selectedPageIndex;
    private string _versionText = AppText.AppVersion;

    public FormatAtViewModel FormatAtVM { get; }
    public YukleViewModel YukleVM { get; }
    public OptimizasyonViewModel OptimizasyonVM { get; }
    public InfoViewModel InfoVM { get; }

    public string VersionText
    {
        get => _versionText;
        set => SetProperty(ref _versionText, value);
    }

    public BaseViewModel CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public int SelectedPageIndex
    {
        get => _selectedPageIndex;
        set
        {
            if (SetProperty(ref _selectedPageIndex, value))
            {
                CurrentPage = value switch
                {
                    0 => FormatAtVM,
                    1 => YukleVM,
                    2 => OptimizasyonVM,
                    3 => InfoVM,
                    _ => FormatAtVM
                };
            }
        }
    }

    public ICommand NavigateFormatAtCommand { get; }
    public ICommand NavigateYukleCommand { get; }
    public ICommand NavigateOptimizasyonCommand { get; }
    public ICommand NavigateInfoCommand { get; }

    public MainViewModel()
    {
        FormatAtVM = new FormatAtViewModel();
        YukleVM = new YukleViewModel();
        OptimizasyonVM = new OptimizasyonViewModel();
        InfoVM = new InfoViewModel();
        _currentPage = FormatAtVM;

        NavigateFormatAtCommand = new RelayCommand(() => SelectedPageIndex = 0);
        NavigateYukleCommand = new RelayCommand(() => SelectedPageIndex = 1);
        NavigateOptimizasyonCommand = new RelayCommand(() => SelectedPageIndex = 2);
        NavigateInfoCommand = new RelayCommand(() => SelectedPageIndex = 3);

        // Handle resume mode
        if (App.IsResumeMode && App.ResumePackagePath != null)
        {
            SelectedPageIndex = 1;
            YukleVM.HandleResume(App.ResumePackagePath);
        }

        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        var result = await UpdateService.CheckForUpdatesAsync();
        if (result is not { UpdateAvailable: true })
        {
            return;
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            VersionText = $"{AppText.AppVersion} • {AppText.UpdateAvailableBadge(result.LatestVersion)}";

            var choice = MessageBox.Show(
                AppText.UpdateAvailableMessage(result.LatestVersion, AppText.AppVersion),
                AppText.UpdateAvailableTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (choice == MessageBoxResult.Yes)
            {
                UpdateService.OpenReleasePage(result.ReleaseUrl);
            }
        });
    }
}
