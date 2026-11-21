using System.Text.Json.Serialization;

namespace WirePack.Core.Project;

/// <summary>
/// Исходная группа проводов одинакового диаметра.
/// </summary>
public sealed class WireGroupDto
{
    /// <summary>
    /// Наружный диаметр провода с изоляцией, мм.
    /// </summary>
    [JsonRequired]
    public double Diameter { get; init; }

    /// <summary>
    /// Количество проводов в группе.
    /// </summary>
    [JsonRequired]
    public int Count { get; init; }
}
