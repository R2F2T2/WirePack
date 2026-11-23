using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using WirePack.App.Project;
using WirePack.App.Settings;
using WirePack.App.ViewModels;
using WirePack.App.Views;

namespace WirePack.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settings = UserSettingsService.Load();

            if (!settings.SkipWelcomeWindow)
            {
                var welcomeWindow = new WelcomeWindow();
                desktop.MainWindow = welcomeWindow;
                welcomeWindow.Show();

                welcomeWindow.Closed += (sender, args) =>
                {
                    if (welcomeWindow.StartClicked)
                    {
                        if (welcomeWindow.IsDoNotShowAgain)
                        {
                            settings.SkipWelcomeWindow = true;
                            UserSettingsService.Save(settings);
                        }

                        var mainWindow = CreateMainWindow();
                        desktop.MainWindow = mainWindow;
                        mainWindow.Show();
                    }
                    else
                    {
                        desktop.Shutdown();
                    }
                };
            }
            else
            {
                desktop.MainWindow = CreateMainWindow();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainWindow CreateMainWindow()
    {
        var window = new MainWindow();
        var dialogService = new ProjectDialogService(window);
        window.DataContext = new MainViewModel(new ProjectFileService(), dialogService);
        return window;
    }
}