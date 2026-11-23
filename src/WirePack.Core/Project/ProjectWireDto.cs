using System.Text.Json.Serialization;

namespace WirePack.Core.Project;

/// <summary>
/// Провод сохранённого варианта компоновки.
/// </summary>
public sealed class ProjectWireDto
{
    /// <summary>
    /// Идентификатор исходной группы, к которой относится провод.
    /// </summary>
    [JsonRequired]
    public string GroupId { get; init; } = string.Empty;

    /// <summary>
    /// Наружный диаметр провода с изоляцией, мм.
    /// </summary>
    [JsonRequired]
    public double Diameter { get; init; }

    /// <summary>
    /// Координаты центра провода, мм.
    /// </summary>
    [JsonRequired]
    public ProjectPoint Center { get; init; }
}
