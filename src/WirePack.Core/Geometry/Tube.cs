namespace WirePack.Core.Geometry;

/// <summary>
/// Защитная трубка, представленная внутренней окружностью.
/// </summary>
public readonly record struct Tube
{
    public Tube(Point center, double innerRadius)
    {
        if (!center.IsFinite)
            throw new ArgumentOutOfRangeException(nameof(center), "Центр трубки должен быть конечной точкой.");

        if (!double.IsFinite(innerRadius) || innerRadius < 0d)
            throw new ArgumentOutOfRangeException(nameof(innerRadius), "Внутренний радиус трубки должен быть неотрицательным конечным числом.");

        Center = center;
        InnerRadius = innerRadius;
    }

    /// <summary>
    /// Центр внутренней окружности трубки.
    /// </summary>
    public Point Center { get; }

    /// <summary>
    /// Внутренний радиус трубки.
    /// </summary>
    public double InnerRadius { get; }

    /// <summary>
    /// Внутренний диаметр трубки.
    /// </summary>
    public double InnerDiameter => 2d * InnerRadius;

    /// <summary>
    /// Внутренняя окружность трубки.
    /// </summary>
    public Circle InnerCircle => new(Center, InnerRadius);

    /// <summary>
    /// Создаёт трубку по внутреннему диаметру.
    /// </summary>
    public static Tube FromInnerDiameter(Point center, double innerDiameter)
    {
        if (!double.IsFinite(innerDiameter) || innerDiameter < 0d)
            throw new ArgumentOutOfRangeException(nameof(innerDiameter), "Внутренний диаметр трубки должен быть неотрицательным конечным числом.");

        return new Tube(center, innerDiameter / 2d);
    }
}
