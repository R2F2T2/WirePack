namespace WirePack.Core.Geometry;

/// <summary>
/// Базовые геометрические операции над точками, окружностями, проводами и трубкой.
/// </summary>
public static class GeometryOperations
{
    /// <summary>
    /// Возвращает расстояние между точками.
    /// </summary>
    public static double Distance(Point first, Point second) => first.DistanceTo(second);

    /// <summary>
    /// Возвращает квадрат расстояния между точками.
    /// </summary>
    public static double SquaredDistance(Point first, Point second) => first.DistanceSquaredTo(second);

    /// <summary>
    /// Проверяет, что окружности пересекаются. Внешнее касание пересечением не считается.
    /// </summary>
    public static bool CirclesIntersect(Circle first, Circle second)
    {
        double distance = first.Center.DistanceTo(second.Center);
        double radiusDifference = Math.Abs(first.Radius - second.Radius);

        if (distance <= radiusDifference)
            return true;

        return GeometryTolerance.IsLess(distance, first.Radius + second.Radius);
    }

    /// <summary>
    /// Проверяет внешнее касание окружностей с учётом допуска.
    /// </summary>
    public static bool CirclesTouchExternally(Circle first, Circle second)
    {
        double distance = first.Center.DistanceTo(second.Center);
        return GeometryTolerance.AreClose(distance, first.Radius + second.Radius);
    }

    /// <summary>
    /// Проверяет, что внутренняя окружность полностью находится внутри внешней.
    /// Внутреннее касание считается допустимым.
    /// </summary>
    public static bool CircleContains(Circle outer, Circle inner)
    {
        double distance = outer.Center.DistanceTo(inner.Center);
        return GeometryTolerance.IsLessOrEqual(distance + inner.Radius, outer.Radius);
    }

    /// <summary>
    /// Проверяет пересечение двух проводов.
    /// </summary>
    public static bool WiresIntersect(Wire first, Wire second) =>
        CirclesIntersect(first.Circle, second.Circle);

    /// <summary>
    /// Проверяет, что провод полностью находится внутри трубки.
    /// </summary>
    public static bool WireInsideTube(Wire wire, Tube tube) =>
        CircleContains(tube.InnerCircle, wire.Circle);

    /// <summary>
    /// Проверяет касание провода внутренней стенки трубки.
    /// </summary>
    public static bool WireTouchesTube(Wire wire, Tube tube)
    {
        double distance = wire.Center.DistanceTo(tube.Center);
        return GeometryTolerance.AreClose(distance + wire.Radius, tube.InnerRadius);
    }
}
