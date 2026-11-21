namespace WirePack.Core.Tests;

public class GeometryOperationTests
{
    private static Circle Circle(double x, double y, double radius) => new(new Point(x, y), radius);

    [Fact]
    public void CirclesIntersect_Overlapping_ReturnsTrue()
    {
        Assert.True(GeometryOperations.CirclesIntersect(
            Circle(0d, 0d, 1d),
            Circle(1.5d, 0d, 1d)));
    }

    [Fact]
    public void CirclesIntersect_Separated_ReturnsFalse()
    {
        Assert.False(GeometryOperations.CirclesIntersect(
            Circle(0d, 0d, 1d),
            Circle(3d, 0d, 1d)));
    }

    [Fact]
    public void CirclesIntersect_ExternalTouching_ReturnsFalse()
    {
        var first = Circle(0d, 0d, 1d);
        var second = Circle(2d, 0d, 1d);

        Assert.False(GeometryOperations.CirclesIntersect(first, second));
        Assert.True(GeometryOperations.CirclesTouchExternally(first, second));
    }

    [Fact]
    public void CirclesIntersect_Contained_ReturnsTrue()
    {
        Assert.True(GeometryOperations.CirclesIntersect(
            Circle(0d, 0d, 5d),
            Circle(3d, 0d, 1d)));
    }

    [Fact]
    public void CircleContains_Inside_ReturnsTrue()
    {
        Assert.True(GeometryOperations.CircleContains(
            Circle(0d, 0d, 5d),
            Circle(4d, 0d, 1d)));
    }

    [Fact]
    public void CircleContains_Outside_ReturnsFalse()
    {
        Assert.False(GeometryOperations.CircleContains(
            Circle(0d, 0d, 5d),
            Circle(4.1d, 0d, 1d)));
    }

    [Fact]
    public void WireInsideTube_TouchingFromInside_ReturnsTrue()
    {
        var tube = new Tube(new Point(0d, 0d), 10d);
        var wire = TestHelpers.Wire(9d, 0d, 2d);

        Assert.True(GeometryOperations.WireInsideTube(wire, tube));
        Assert.True(GeometryOperations.WireTouchesTube(wire, tube));
    }
}
