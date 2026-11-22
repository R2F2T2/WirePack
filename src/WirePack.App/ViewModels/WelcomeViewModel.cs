using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WirePack.App.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isDoNotShowAgainChecked;

    public event Action? StartRequested;

    [RelayCommand]
    private void StartWorking()
    {
        StartRequested?.Invoke();
    }
}
