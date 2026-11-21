using WirePack.Core.Project;

namespace WirePack.App.Project;

/// <summary>
/// Результат открытия файла проекта.
/// </summary>
public sealed class ProjectLoadResult
{
    private ProjectLoadResult(
        ProjectDocument? document,
        string? errorMessage,
        IReadOnlyList<ProjectValidationIssue> issues)
    {
        Document = document;
        ErrorMessage = errorMessage;
        Issues = issues;
    }

    /// <summary>
    /// Загруженный проект. Для неуспешного результата равен <see langword="null"/>.
    /// </summary>
    public ProjectDocument? Document { get; }

    /// <summary>
    /// Сообщение об ошибке открытия файла.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Нарушения, обнаруженные при проверке данных.
    /// </summary>
    public IReadOnlyList<ProjectValidationIssue> Issues { get; }

    /// <summary>
    /// Признак успешного открытия проекта.
    /// </summary>
    public bool IsSuccess => Document is not null && ErrorMessage is null;

    /// <summary>
    /// Создаёт успешный результат.
    /// </summary>
    public static ProjectLoadResult Success(
        ProjectDocument document,
        IReadOnlyList<ProjectValidationIssue>? issues = null) =>
        new(document, null, issues ?? Array.Empty<ProjectValidationIssue>());

    /// <summary>
    /// Создаёт неуспешный результат.
    /// </summary>
    public static ProjectLoadResult Failure(
        string errorMessage,
        IReadOnlyList<ProjectValidationIssue>? issues = null) =>
        new(null, errorMessage, issues ?? Array.Empty<ProjectValidationIssue>());
}
