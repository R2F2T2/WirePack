namespace WirePack.Core.Tests;

public class ToleranceTests
{
    [Fact]
    public void IsZero_UsesEps()
    {
        Assert.True(GeometryTolerance.IsZero(GeometryTolerance.Eps));
        Assert.False(GeometryTolerance.IsZero(GeometryTolerance.Eps * 2d));
    }

    [Fact]
    public void TouchingWithinTolerance_IsAllowed()
    {
        var first = new Circle(new Point(0d, 0d), 1d);
        var second = new Circle(new Point(2d - GeometryTolerance.Eps * 0.5d, 0d), 1d);

        Assert.False(GeometryOperations.CirclesIntersect(first, second));
        Assert.True(GeometryOperations.CirclesTouchExternally(first, second));
    }

    [Fact]
    public void OverlapExceedingTolerance_IsRejected()
    {
        var first = new Circle(new Point(0d, 0d), 1d);
        var second = new Circle(new Point(2d - GeometryTolerance.Eps * 2d, 0d), 1d);

        Assert.True(GeometryOperations.CirclesIntersect(first, second));
    }

    [Fact]
    public void ContainmentWithinTolerance_IsAllowed()
    {
        var tube = new Tube(new Point(0d, 0d), 5d);
        var wire = new Wire(new Point(4d + GeometryTolerance.Eps * 0.5d, 0d), 2d);

        Assert.True(GeometryOperations.WireInsideTube(wire, tube));
    }

    [Fact]
    public void ContainmentExceedingTolerance_IsRejected()
    {
        var tube = new Tube(new Point(0d, 0d), 5d);
        var wire = new Wire(new Point(4d + GeometryTolerance.Eps * 2d, 0d), 2d);

        Assert.False(GeometryOperations.WireInsideTube(wire, tube));
    }
}
