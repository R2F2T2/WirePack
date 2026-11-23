namespace WirePack.Core.Project;

/// <summary>
/// Важность нарушения в данных проекта.
/// </summary>
public enum ProjectValidationSeverity
{
    /// <summary>
    /// Предупреждение, не препятствующее открытию проекта.
    /// </summary>
    Warning,

    /// <summary>
    /// Ошибка, из-за которой данные варианта или проекта непригодны.
    /// </summary>
    Error
}

/// <summary>
/// Вид нарушения в данных проекта.
/// </summary>
public enum ProjectValidationIssueKind
{
    /// <summary>
    /// Версия схемы не поддерживается.
    /// </summary>
    UnsupportedFormatVersion,

    /// <summary>
    /// Группы проводов отсутствуют.
    /// </summary>
    MissingWireGroups,

    /// <summary>
    /// Описание группы проводов отсутствует.
    /// </summary>
    InvalidWireGroup,

    /// <summary>
    /// У группы проводов отсутствует идентификатор.
    /// </summary>
    MissingWireGroupId,

    /// <summary>
    /// Идентификатор группы проводов повторяется.
    /// </summary>
    DuplicateWireGroupId,

    /// <summary>
    /// Некорректный диаметр провода в группе.
    /// </summary>
    InvalidWireGroupDiameter,

    /// <summary>
    /// Некорректное количество проводов в группе.
    /// </summary>
    InvalidWireGroupCount,

    /// <summary>
    /// Некорректная строка цвета группы.
    /// </summary>
    InvalidWireGroupColor,

    /// <summary>
    /// Превышено максимальное количество проводов.
    /// </summary>
    WireCountExceeded,

    /// <summary>
    /// Описание варианта отсутствует.
    /// </summary>
    InvalidVariant,

    /// <summary>
    /// У варианта отсутствует идентификатор.
    /// </summary>
    MissingVariantId,

    /// <summary>
    /// Идентификатор варианта повторяется.
    /// </summary>
    DuplicateVariantId,

    /// <summary>
    /// Некорректный внутренний диаметр трубки.
    /// </summary>
    InvalidTubeDiameter,

    /// <summary>
    /// Некорректный коэффициент заполнения.
    /// </summary>
    InvalidFillFactor,

    /// <summary>
    /// Некорректные координаты.
    /// </summary>
    InvalidCoordinates,

    /// <summary>
    /// Набор проводов варианта отсутствует или пуст.
    /// </summary>
    InvalidWireSet,

    /// <summary>
    /// Некорректный диаметр провода в варианте.
    /// </summary>
    InvalidWireDiameter,

    /// <summary>
    /// Провод ссылается на отсутствующую группу.
    /// </summary>
    MissingWireGroupReference,

    /// <summary>
    /// Компоновка геометрически недопустима.
    /// </summary>
    InvalidVariantGeometry,

    /// <summary>
    /// Сохранённый центр трубки не совпадает с центром MEC.
    /// </summary>
    MecCenterMismatch,

    /// <summary>
    /// Сохранённый диаметр трубки не совпадает с диаметром MEC.
    /// </summary>
    MecDiameterMismatch,

    /// <summary>
    /// Сохранённые результаты не соответствуют текущим исходным данным.
    /// </summary>
    OutdatedResults
}

/// <summary>
/// Описание одного нарушения в данных проекта.
/// </summary>
public sealed record ProjectValidationIssue(
    ProjectValidationIssueKind Kind,
    ProjectValidationSeverity Severity,
    string Message,
    int? VariantIndex = null,
    string? VariantId = null);
