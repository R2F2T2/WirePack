namespace WirePack.App.Project;

/// <summary>
/// Ошибка чтения или записи файла проекта.
/// </summary>
public sealed class ProjectFileException : Exception
{
    public ProjectFileException(string message)
        : base(message)
    {
    }

    public ProjectFileException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
