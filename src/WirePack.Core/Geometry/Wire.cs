namespace WirePack.Core.Geometry;

/// <summary>
/// Провод с наружным диаметром и положением центра.
/// </summary>
public readonly record struct Wire
{
    public Wire(Point center, double diameter)
    {
        if (!center.IsFinite)
            throw new ArgumentOutOfRangeException(nameof(center), "Центр провода должен быть конечной точкой.");

        if (!double.IsFinite(diameter) || diameter <= 0d)
            throw new ArgumentOutOfRangeException(nameof(diameter), "Диаметр провода должен быть положительным конечным числом.");

        Center = center;
        Diameter = diameter;
    }

    /// <summary>
    /// Центр провода.
    /// </summary>
    public Point Center { get; }

    /// <summary>
    /// Наружный диаметр провода.
    /// </summary>
    public double Diameter { get; }

    /// <summary>
    /// Радиус провода.
    /// </summary>
    public double Radius => Diameter / 2d;

    /// <summary>
    /// Геометрическая окружность провода.
    /// </summary>
    public Circle Circle => new(Center, Radius);

    /// <summary>
    /// Возвращает провод с тем же диаметром и новым центром.
    /// </summary>
    public Wire MoveTo(Point center) => new(center, Diameter);

    /// <summary>
    /// Создаёт провод по радиусу.
    /// </summary>
    public static Wire FromRadius(Point center, double radius)
    {
        if (!double.IsFinite(radius) || radius <= 0d)
            throw new ArgumentOutOfRangeException(nameof(radius), "Радиус провода должен быть положительным конечным числом.");

        return new Wire(center, radius * 2d);
    }
}
