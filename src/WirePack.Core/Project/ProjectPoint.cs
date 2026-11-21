using System.Text.Json.Serialization;

namespace WirePack.Core.Project;

/// <summary>
/// Координаты точки в миллиметрах для модели данных проекта.
/// </summary>
/// <remarks>
/// Тип не относится к геометрическому ядру и используется только для хранения проекта.
/// </remarks>
public readonly record struct ProjectPoint
{
    public ProjectPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Координата по оси X, мм.
    /// </summary>
    [JsonRequired]
    public double X { get; init; }

    /// <summary>
    /// Координата по оси Y, мм.
    /// </summary>
    [JsonRequired]
    public double Y { get; init; }
}
