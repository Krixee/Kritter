using System.Windows.Input;

namespace Kritter.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel _currentPage;
    private int _selectedPageIndex;

    public FormatAtViewModel FormatAtVM { get; }
    public YukleViewModel YukleVM { get; }
    public OptimizasyonViewModel OptimizasyonVM { get; }
    public InfoViewModel InfoVM { get; }

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
    }
}
