using WirePack.Core.Geometry;

namespace WirePack.Optimization.Tests;

public class RelaxationEngineTests
{
    [Fact]
    public void Relax_ThrowsOnNullLayout()
    {
        Assert.Throws<ArgumentNullException>(() => RelaxationEngine.Relax(null!, 5d));
    }

    [Fact]
    public void Relax_AlreadyInsideRadius_ReturnsSuccessful()
    {
        var layout = Layout.FromWires(
            new Wire(new Point(0d, 0d), 2d),
            new Wire(new Point(3d, 0d), 2d));

        var (result, success) = RelaxationEngine.Relax(layout, 5d);

        Assert.True(success);
    }

    [Fact]
    public void Relax_TooSmallTargetRadius_ReturnsUnsuccessful()
    {
        // Два провода диаметром 2 мм не поместятся в радиус 1 мм
        var layout = Layout.FromWires(
            new Wire(Point.Origin, 2d),
            new Wire(new Point(3d, 0d), 2d));

        var (_, success) = RelaxationEngine.Relax(layout, 1d);

        Assert.False(success);
    }

    [Fact]
    public void Relax_RemovesOverlaps()
    {
        // Два провода с центром в одной точке — пересечение
        var layout = Layout.FromWires(
            new Wire(Point.Origin, 2d),
            new Wire(Point.Origin, 2d));

        var (result, success) = RelaxationEngine.Relax(layout, 5d);

        Assert.True(success);
        // Провода не должны пересекаться после релаксации
        var tube = new Tube(Point.Origin, 5d);
        var validation = LayoutValidator.Validate(result, tube);
        Assert.True(validation.IsValid, string.Join("; ", validation.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void Relax_PullsWiresInsideBoundary()
    {
        // Провод, выходящий за радиус
        var layout = Layout.FromWires(
            new Wire(new Point(8d, 0d), 2d),  // центр на 8, радиус 1 — выходит за 7
            new Wire(Point.Origin, 2d));

        var (result, success) = RelaxationEngine.Relax(layout, 3d);

        Assert.True(success);
        var tube = new Tube(Point.Origin, 3d);
        var validation = LayoutValidator.Validate(result, tube);
        Assert.True(validation.IsValid, string.Join("; ", validation.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void Relax_HandlesSingleWire()
    {
        var layout = Layout.FromWires(new Wire(Point.Origin, 2d));

        var (result, success) = RelaxationEngine.Relax(layout, 1.5d);

        Assert.True(success);
        Assert.Equal(1, result.Count);
    }

    [Fact]
    public void Relax_HandlesManyWires()
    {
        // Три провода с шагом 3 — последний чуть за границей радиуса 6,
        // релаксация должна втянуть его внутрь
        var wires = Enumerable.Range(0, 3)
            .Select(i => new Wire(new Point(i * 3d, 0d), 1d))
            .ToArray();
        var layout = new Layout(wires);

        var (result, success) = RelaxationEngine.Relax(layout, 6d);

        Assert.True(success);
        var tube = new Tube(Point.Origin, 6d);
        var validation = LayoutValidator.Validate(result, tube);
        Assert.True(validation.IsValid, string.Join("; ", validation.Issues.Select(i => i.Message)));
    }
}