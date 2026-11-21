namespace WirePack.Core.Geometry;

/// <summary>
/// Вид нарушения геометрической допустимости компоновки.
/// </summary>
public enum ValidationIssueKind
{
    /// <summary>
    /// Провода пересекаются.
    /// </summary>
    WireIntersection,

    /// <summary>
    /// Провод выходит за пределы трубки.
    /// </summary>
    WireOutsideTube,

    /// <summary>
    /// Сохранённый центр трубки не совпадает с центром MEC.
    /// </summary>
    MecCenterMismatch,

    /// <summary>
    /// Сохранённый диаметр трубки не совпадает с диаметром MEC.
    /// </summary>
    MecDiameterMismatch
}

/// <summary>
/// Описание одного нарушения.
/// </summary>
public sealed record ValidationIssue(
    ValidationIssueKind Kind,
    int FirstWireIndex,
    int SecondWireIndex,
    string Message)
{
    /// <summary>
    /// Создаёт нарушение, относящееся к одному проводу.
    /// </summary>
    public static ValidationIssue ForWire(ValidationIssueKind kind, int wireIndex, string message) =>
        new(kind, wireIndex, -1, message);

    /// <summary>
    /// Создаёт нарушение, относящееся к паре проводов.
    /// </summary>
    public static ValidationIssue ForPair(ValidationIssueKind kind, int firstIndex, int secondIndex, string message) =>
        new(kind, firstIndex, secondIndex, message);
}

/// <summary>
/// Результат независимой проверки компоновки.
/// </summary>
public sealed class LayoutValidationResult
{
    /// <summary>
    /// Успешный результат без нарушений.
    /// </summary>
    public static LayoutValidationResult Success { get; } = new(Array.Empty<ValidationIssue>());

    public LayoutValidationResult(IEnumerable<ValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        Issues = issues.ToArray();
    }

    /// <summary>
    /// Найденные нарушения.
    /// </summary>
    public IReadOnlyList<ValidationIssue> Issues { get; }

    /// <summary>
    /// Признак отсутствия нарушений.
    /// </summary>
    public bool IsValid => Issues.Count == 0;
}
