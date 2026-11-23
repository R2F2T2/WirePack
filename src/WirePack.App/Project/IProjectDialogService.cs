namespace WirePack.App.Project;

/// <summary>
/// Решение пользователя по несохранённым изменениям.
/// </summary>
public enum UnsavedChangesChoice
{
    /// <summary>
    /// Отменить действие.
    /// </summary>
    Cancel = 0,

    /// <summary>
    /// Сохранить изменения.
    /// </summary>
    Save = 1,

    /// <summary>
    /// Продолжить без сохранения.
    /// </summary>
    Discard = 2
}

/// <summary>
/// Диалоги выбора файла и подтверждения несохранённых изменений.
/// </summary>
public interface IProjectDialogService
{
    /// <summary>
    /// Запрашивает путь к открываемому проекту.
    /// </summary>
    Task<string?> PickOpenProjectPathAsync();

    /// <summary>
    /// Запрашивает путь для сохранения проекта.
    /// </summary>
    Task<string?> PickSaveProjectPathAsync(string? currentPath);

    /// <summary>
    /// Запрашивает решение по несохранённым изменениям.
    /// </summary>
    Task<UnsavedChangesChoice> ConfirmUnsavedChangesAsync(string actionDescription);
}
