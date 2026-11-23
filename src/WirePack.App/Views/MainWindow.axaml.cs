using Avalonia.Controls;
using Avalonia.Interactivity;
using WirePack.App.ViewModels;

namespace WirePack.App.Views;

public partial class MainWindow : Window
{
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnWindowClosing;
    }

    private void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.ShowDialog(this);
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closeConfirmed)
            return;

        if (DataContext is not MainViewModel viewModel || !viewModel.IsDirty)
            return;

        e.Cancel = true;

        bool canClose = await viewModel.ConfirmCloseAsync();
        if (!canClose)
            return;

        _closeConfirmed = true;
        Close();
    }
}