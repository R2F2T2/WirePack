namespace WirePack.Core.Tests;

public class PointTests
{
    [Fact]
    public void Distance_ReturnsEuclideanDistance()
    {
        var first = new Point(0d, 0d);
        var second = new Point(3d, 4d);

        TestHelpers.AssertClose(5d, first.DistanceTo(second));
        TestHelpers.AssertClose(25d, first.DistanceSquaredTo(second));
    }

    [Fact]
    public void Translate_ShiftsCoordinates()
    {
        var point = new Point(1d, -2d);

        Assert.Equal(new Point(4d, 2d), point.Translate(3d, 4d));
        Assert.Equal(new Point(4d, 2d), point.Translate(new Point(3d, 4d)));
    }
}
