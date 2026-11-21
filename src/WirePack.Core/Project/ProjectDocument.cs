using System.Text.Json.Serialization;

namespace WirePack.Core.Project;

/// <summary>
/// Проект WirePack в формате модели данных, не зависящей от геометрии и UI.
/// </summary>
public sealed class ProjectDocument
{
    /// <summary>
    /// Версия формата данных проекта.
    /// </summary>
    [JsonRequired]
    public int FormatVersion { get; init; } = ProjectFormat.CurrentVersion;

    /// <summary>
    /// Исходные группы проводов.
    /// </summary>
    [JsonRequired]
    public List<WireGroupDto> WireGroups { get; init; } = [];

    /// <summary>
    /// Найденные варианты компоновки. Может быть пустым, если расчёт не выполнялся.
    /// </summary>
    public List<LayoutVariantDto> Variants { get; init; } = [];

    /// <summary>
    /// Возвращает копию проекта с другим набором вариантов.
    /// </summary>
    public ProjectDocument WithVariants(IEnumerable<LayoutVariantDto> variants)
    {
        ArgumentNullException.ThrowIfNull(variants);

        return new ProjectDocument
        {
            FormatVersion = FormatVersion,
            WireGroups = WireGroups,
            Variants = variants.ToList()
        };
    }
}
