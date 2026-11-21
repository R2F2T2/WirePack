using System.Text.Json.Serialization;

namespace WirePack.Core.Project;

/// <summary>
/// Сохранённый вариант компоновки проводов.
/// </summary>
public sealed class LayoutVariantDto
{
    /// <summary>
    /// Уникальный идентификатор варианта.
    /// </summary>
    [JsonRequired]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Внутренний диаметр защитной трубки, мм.
    /// </summary>
    [JsonRequired]
    public double TubeDiameter { get; init; }

    /// <summary>
    /// Координаты центра защитной трубки, мм.
    /// </summary>
    [JsonRequired]
    public ProjectPoint TubeCenter { get; init; }

    /// <summary>
    /// Провода компоновки.
    /// </summary>
    [JsonRequired]
    public List<ProjectWireDto> Wires { get; init; } = [];
}
