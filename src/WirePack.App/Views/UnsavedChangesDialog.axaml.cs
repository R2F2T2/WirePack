using Avalonia.Controls;
using Avalonia.Interactivity;
using WirePack.App.Project;

namespace WirePack.App.Views;

public partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialog()
        : this("закрытие приложения")
    {
    }

    public UnsavedChangesDialog(string actionDescription)
    {
        InitializeComponent();
        MessageText.Text =
            $"В проекте есть несохранённые изменения. Сохранить их перед {actionDescription}?";
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e) =>
        Close(UnsavedChangesChoice.Save);

    private void OnDiscardClick(object? sender, RoutedEventArgs e) =>
        Close(UnsavedChangesChoice.Discard);

    private void OnCancelClick(object? sender, RoutedEventArgs e) =>
        Close(UnsavedChangesChoice.Cancel);
}
