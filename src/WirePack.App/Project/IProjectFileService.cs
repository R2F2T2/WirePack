using WirePack.Core.Project;

namespace WirePack.App.Project;

/// <summary>
/// Асинхронное сохранение и открытие файлов проекта.
/// </summary>
public interface IProjectFileService
{
    /// <summary>
    /// Загружает проект из файла.
    /// </summary>
    Task<ProjectLoadResult> LoadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет проект в файл.
    /// </summary>
    Task SaveAsync(string path, ProjectDocument document, CancellationToken cancellationToken = default);
}
