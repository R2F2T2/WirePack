using Avalonia.Controls;
using WirePack.App.ViewModels;

namespace WirePack.App.Views;

public partial class WelcomeWindow : Window
{
    private readonly WelcomeViewModel _viewModel;

    public bool StartClicked { get; private set; }
    public bool IsDoNotShowAgain => _viewModel.IsDoNotShowAgainChecked;

    public WelcomeWindow()
    {
        InitializeComponent();
        _viewModel = new WelcomeViewModel();
        DataContext = _viewModel;
        _viewModel.StartRequested += () =>
        {
            StartClicked = true;
            Close();
        };
    }
}
