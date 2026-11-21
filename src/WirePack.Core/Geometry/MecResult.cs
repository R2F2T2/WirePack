namespace WirePack.Core.Geometry;

/// <summary>
/// Результат вычисления минимальной охватывающей окружности.
/// </summary>
public sealed class MecResult
{
    public MecResult(Point center, double radius, IEnumerable<int>? supportingWireIndices = null)
    {
        if (!center.IsFinite)
            throw new ArgumentOutOfRangeException(nameof(center), "Центр MEC должен быть конечной точкой.");

        if (!double.IsFinite(radius) || radius < 0d)
            throw new ArgumentOutOfRangeException(nameof(radius), "Радиус MEC должен быть неотрицательным конечным числом.");

        Center = center;
        Radius = radius;
        SupportingWireIndices = supportingWireIndices?.ToArray() ?? Array.Empty<int>();
    }

    /// <summary>
    /// Центр MEC.
    /// </summary>
    public Point Center { get; }

    /// <summary>
    /// Радиус MEC.
    /// </summary>
    public double Radius { get; }

    /// <summary>
    /// Диаметр MEC.
    /// </summary>
    public double Diameter => 2d * Radius;

    /// <summary>
    /// Индексы опорных проводов, определяющих MEC.
    /// </summary>
    public IReadOnlyList<int> SupportingWireIndices { get; }

    /// <summary>
    /// Возвращает компоновку с центром MEC в начале координат.
    /// </summary>
    public Layout Recenter(Layout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        return layout.Translate(-Center.X, -Center.Y);
    }

    /// <summary>
    /// Переводит точку в систему координат с центром MEC в начале координат.
    /// </summary>
    public Point ToLocal(Point point) => point.Translate(-Center.X, -Center.Y);
}
