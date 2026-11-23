using WirePack.Core.Project;

namespace WirePack.App.Project;

/// <summary>
/// Файловые операции с проектом WirePack.
/// </summary>
public sealed class ProjectFileService : IProjectFileService
{
    /// <summary>
    /// Загружает проект из файла.
    /// </summary>
    public async Task<ProjectLoadResult> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ProjectLoadResult.Failure("Не указан путь к файлу проекта.");

        try
        {
            string json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return ProjectFileSerializer.Deserialize(json);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return ProjectLoadResult.Failure($"Не удалось прочитать файл проекта: {exception.Message}");
        }
    }

    /// <summary>
    /// Сохраняет проект в файл.
    /// </summary>
    /// <exception cref="ProjectFileException">Файл не удалось записать.</exception>
    public async Task SaveAsync(
        string path,
        ProjectDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(document);

        string json = ProjectFileSerializer.Serialize(document);

        try
        {
            await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new ProjectFileException($"Не удалось сохранить файл проекта: {exception.Message}", exception);
        }
    }
}
