namespace WirePack.Core.Geometry;

/// <summary>
/// Точка на плоскости. Координаты задаются в миллиметрах.
/// </summary>
public readonly record struct Point(double X, double Y)
{
    /// <summary>
    /// Начало координат.
    /// </summary>
    public static Point Origin => new(0d, 0d);

    /// <summary>
    /// Проверяет, что обе координаты являются конечными числами.
    /// </summary>
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y);

    /// <summary>
    /// Возвращает квадрат расстояния до другой точки.
    /// </summary>
    public double DistanceSquaredTo(Point other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Возвращает расстояние до другой точки.
    /// </summary>
    public double DistanceTo(Point other) => Math.Sqrt(DistanceSquaredTo(other));

    /// <summary>
    /// Возвращает точку, смещённую на заданные приращения координат.
    /// </summary>
    public Point Translate(double dx, double dy) => new(X + dx, Y + dy);

    /// <summary>
    /// Возвращает точку, смещённую на заданный вектор.
    /// </summary>
    public Point Translate(Point offset) => new(X + offset.X, Y + offset.Y);

    /// <summary>
    /// Возвращает расстояние между двумя точками.
    /// </summary>
    public static double Distance(Point first, Point second) => first.DistanceTo(second);
}
