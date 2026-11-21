namespace WirePack.Core.Project;

/// <summary>
/// Результат проверки данных проекта.
/// </summary>
public sealed class ProjectValidationResult
{
    /// <summary>
    /// Успешный результат без нарушений.
    /// </summary>
    public static ProjectValidationResult Success { get; } = new(Array.Empty<ProjectValidationIssue>());

    public ProjectValidationResult(IEnumerable<ProjectValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        Issues = issues.ToArray();
    }

    /// <summary>
    /// Найденные нарушения.
    /// </summary>
    public IReadOnlyList<ProjectValidationIssue> Issues { get; }

    /// <summary>
    /// Признак отсутствия ошибок. Предупреждения не делают проект недействительным.
    /// </summary>
    public bool IsValid => !HasErrors;

    /// <summary>
    /// Признак наличия ошибок.
    /// </summary>
    public bool HasErrors => Issues.Any(issue => issue.Severity == ProjectValidationSeverity.Error);

    /// <summary>
    /// Признак наличия предупреждений.
    /// </summary>
    public bool HasWarnings => Issues.Any(issue => issue.Severity == ProjectValidationSeverity.Warning);

    /// <summary>
    /// Признак ошибок уровня проекта, из-за которых нельзя использовать исходные данные.
    /// </summary>
    public bool HasFatalErrors =>
        Issues.Any(issue =>
            issue.Severity == ProjectValidationSeverity.Error &&
            issue.VariantIndex is null);

    /// <summary>
    /// Признак наличия устаревших результатов расчёта.
    /// </summary>
    public bool HasOutdatedResults =>
        Issues.Any(issue => issue.Kind == ProjectValidationIssueKind.OutdatedResults);
}
