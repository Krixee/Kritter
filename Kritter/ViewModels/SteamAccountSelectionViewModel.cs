using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Kritter.Models;

namespace Kritter.ViewModels;

public class SteamAccountSelectionViewModel : BaseViewModel
{
    private SteamUserAccount? _selectedAccount;

    public ObservableCollection<SteamUserAccount> Accounts { get; } = new();

    public SteamUserAccount? SelectedAccount
    {
        get => _selectedAccount;
        set => SetProperty(ref _selectedAccount, value);
    }

    public SteamAccountSelectionViewModel(IEnumerable<SteamUserAccount> accounts)
    {
        foreach (var account in accounts)
        {
            Accounts.Add(account);
        }

        SelectedAccount = Accounts.FirstOrDefault(a => a.IsMostRecent) ?? Accounts.FirstOrDefault();
    }
}
