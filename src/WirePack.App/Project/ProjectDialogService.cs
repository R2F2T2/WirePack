using Avalonia.Controls;
using Avalonia.Platform.Storage;
using WirePack.App.Views;

namespace WirePack.App.Project;

/// <summary>
/// Диалоги проекта на основе Avalonia StorageProvider.
/// </summary>
public sealed class ProjectDialogService : IProjectDialogService
{
    /// <summary>
    /// Расширение файла проекта.
    /// </summary>
    public const string ProjectExtension = ".wpack";

    private static readonly FilePickerFileType ProjectFileType = new("Проект WirePack")
    {
        Patterns = new[] { "*.wpack" },
        MimeTypes = new[] { "application/json" },
        AppleUniformTypeIdentifiers = new[] { "public.json" }
    };

    private readonly Window _owner;

    public ProjectDialogService(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _owner = owner;
    }

    /// <inheritdoc />
    public async Task<string?> PickOpenProjectPathAsync()
    {
        IReadOnlyList<IStorageFile> files = await _owner.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Открыть проект WirePack",
                AllowMultiple = false,
                FileTypeFilter = new[] { ProjectFileType }
            }).ConfigureAwait(true);

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    /// <inheritdoc />
    public async Task<string?> PickSaveProjectPathAsync(string? currentPath)
    {
        string suggestedName = string.IsNullOrWhiteSpace(currentPath)
            ? $"Проект{ProjectExtension}"
            : Path.GetFileName(currentPath);

        IStorageFile? file = await _owner.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Сохранить проект WirePack",
                SuggestedFileName = suggestedName,
                DefaultExtension = "wpack",
                FileTypeChoices = new[] { ProjectFileType },
                ShowOverwritePrompt = true
            }).ConfigureAwait(true);

        return file?.TryGetLocalPath();
    }

    /// <inheritdoc />
    public Task<UnsavedChangesChoice> ConfirmUnsavedChangesAsync(string actionDescription)
    {
        var dialog = new UnsavedChangesDialog(actionDescription);
        return dialog.ShowDialog<UnsavedChangesChoice>(_owner);
    }
}
