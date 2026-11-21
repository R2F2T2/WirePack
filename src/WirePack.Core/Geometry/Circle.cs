namespace WirePack.Core.Geometry;

/// <summary>
/// Окружность на плоскости.
/// </summary>
public readonly record struct Circle
{
    public Circle(Point center, double radius)
    {
        if (!center.IsFinite)
            throw new ArgumentOutOfRangeException(nameof(center), "Центр окружности должен быть конечной точкой.");

        if (!double.IsFinite(radius) || radius < 0d)
            throw new ArgumentOutOfRangeException(nameof(radius), "Радиус окружности должен быть неотрицательным конечным числом.");

        Center = center;
        Radius = radius;
    }

    /// <summary>
    /// Центр окружности.
    /// </summary>
    public Point Center { get; }

    /// <summary>
    /// Радиус окружности.
    /// </summary>
    public double Radius { get; }

    /// <summary>
    /// Диаметр окружности.
    /// </summary>
    public double Diameter => 2d * Radius;

    /// <summary>
    /// Создаёт окружность по диаметру.
    /// </summary>
    public static Circle FromDiameter(Point center, double diameter)
    {
        if (!double.IsFinite(diameter) || diameter < 0d)
            throw new ArgumentOutOfRangeException(nameof(diameter), "Диаметр окружности должен быть неотрицательным конечным числом.");

        return new Circle(center, diameter / 2d);
    }
}
