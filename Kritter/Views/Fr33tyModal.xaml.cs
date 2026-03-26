using System.Windows;

namespace Kritter.Views;

public partial class Fr33tyModal : Window
{
    public Fr33tyModal()
    {
        InitializeComponent();
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
