namespace WirePack.Core.Tests;

internal static class TestHelpers
{
    public const double Tolerance = 1e-6;

    public static Wire Wire(double x, double y, double diameter) =>
        new(new Point(x, y), diameter);

    public static Tube Tube(double x, double y, double innerDiameter) =>
        new(new Point(x, y), innerDiameter / 2d);

    public static void AssertClose(double expected, double actual, double tolerance = Tolerance)
    {
        Assert.True(
            Math.Abs(expected - actual) <= tolerance,
            $"Ожидалось {expected:G17}, получено {actual:G17}; допуск {tolerance:G17}.");
    }

    public static void AssertPointClose(Point expected, Point actual, double tolerance = Tolerance)
    {
        AssertClose(expected.X, actual.X, tolerance);
        AssertClose(expected.Y, actual.Y, tolerance);
    }

    public static void AssertEnclosesAll(MecResult mec, Layout layout)
    {
        for (int i = 0; i < layout.Count; i++)
        {
            double value = mec.Center.DistanceTo(layout[i].Center) + layout[i].Radius;
            Assert.True(
                value <= mec.Radius + GeometryTolerance.Eps,
                $"Провод {i} не охвачен MEC: значение {value:G17}, радиус {mec.Radius:G17}.");
        }
    }
}
