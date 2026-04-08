using System.Windows;
using Kritter.ViewModels;

namespace Kritter.Views;

public partial class SteamAccountSelectionWindow : Window
{
    public SteamAccountSelectionWindow()
    {
        InitializeComponent();
    }

    public SteamAccountSelectionViewModel? ViewModel => DataContext as SteamAccountSelectionViewModel;

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedAccount == null)
        {
            MessageBox.Show("Lütfen bir Steam hesabı seçin.", "Kritter", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
