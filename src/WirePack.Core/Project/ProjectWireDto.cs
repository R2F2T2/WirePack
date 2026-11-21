using System.Text.Json.Serialization;

namespace WirePack.Core.Project;

/// <summary>
/// Провод сохранённого варианта компоновки.
/// </summary>
public sealed class ProjectWireDto
{
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
