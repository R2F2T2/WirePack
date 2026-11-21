using WirePack.Core.Geometry;

namespace WirePack.Optimization.Tests;

/// <summary>
/// Наборы проводов разного состава для тестирования оптимизатора.
/// </summary>
public static class TestWireSets
{
    /// <summary>
    /// Три провода разных диаметров (1, 1.5, 2 мм).
    /// </summary>
    public static IReadOnlyList<Wire> ThreeWires { get; } = new[]
    {
        new Wire(Point.Origin, 1d),
        new Wire(Point.Origin, 1.5d),
        new Wire(Point.Origin, 2d),
    };

    /// <summary>
    /// Восемь проводов разных диаметров (1–5 мм с повторами).
    /// </summary>
    public static IReadOnlyList<Wire> MixedWires { get; } = new[]
    {
        new Wire(Point.Origin, 1d),
        new Wire(Point.Origin, 1.5d),
        new Wire(Point.Origin, 2d),
        new Wire(Point.Origin, 2.5d),
        new Wire(Point.Origin, 3d),
        new Wire(Point.Origin, 4d),
        new Wire(Point.Origin, 5d),
        new Wire(Point.Origin, 1d),
    };

    /// <summary>
    /// Два провода одинакового диаметра.
    /// </summary>
    public static IReadOnlyList<Wire> TwoEqualWires { get; } = new[]
    {
        new Wire(Point.Origin, 2d),
        new Wire(Point.Origin, 2d),
    };

    /// <summary>
    /// Сто проводов с диаметрами 1, 1.5, 2, 2.5, 3 мм (циклически).
    /// </summary>
    public static IReadOnlyList<Wire> HundredWires { get; } = Enumerable.Range(0, 100)
        .Select(i => new Wire(Point.Origin, 1d + (i % 5) * 0.5d))
        .ToArray();

    /// <summary>
    /// Создаёт набор проводов одинакового диаметра заданного количества.
    /// </summary>
    public static IReadOnlyList<Wire> UniformWires(int count, double diameter = 2d)
    {
        return Enumerable.Range(0, count)
            .Select(_ => new Wire(Point.Origin, diameter))
            .ToArray();
    }
}