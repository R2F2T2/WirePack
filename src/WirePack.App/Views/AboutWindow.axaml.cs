using System.Reflection;
using Avalonia.Interactivity;
using Avalonia.Controls;

namespace WirePack.App.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        LoadVersionInfo();
    }

    private void LoadVersionInfo()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var version = informationalVersion ?? assembly.GetName().Version?.ToString() ?? "0.2.0";
            
            // Clean git hash suffix if present (e.g. 0.2.0+commit)
            if (version.Contains('+'))
            {
                version = version.Substring(0, version.IndexOf('+'));
            }

            VersionText.Text = $"Версия {version}";
        }
        catch
        {
            VersionText.Text = "Версия 0.2.0";
        }
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
