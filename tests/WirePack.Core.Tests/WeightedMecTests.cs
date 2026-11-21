namespace WirePack.Core.Tests;

public class WeightedMecTests
{
    private static readonly Func<Layout, MecResult>[] Solvers =
    {
        NumericalWeightedMecSolver.Solve,
        ExactWeightedMecSolver.Solve
    };

    [Fact]
    public void SingleWire_ReturnsWireCircle()
    {
        var layout = Layout.FromWires(TestHelpers.Wire(3d, 4d, 4d));
        var expectedCenter = new Point(3d, 4d);

        foreach (Func<Layout, MecResult> solver in Solvers)
        {
            MecResult mec = solver(layout);
            TestHelpers.AssertPointClose(expectedCenter, mec.Center);
            TestHelpers.AssertClose(2d, mec.Radius);
            TestHelpers.AssertEnclosesAll(mec, layout);
        }
    }

    [Fact]
    public void TwoEqualWires_ReturnsMidpointAndSumHalf()
    {
        var layout = Layout.FromWires(
            TestHelpers.Wire(-1d, 0d, 2d),
            TestHelpers.Wire(1d, 0d, 2d));

        foreach (Func<Layout, MecResult> solver in Solvers)
        {
            MecResult mec = solver(layout);
            TestHelpers.AssertPointClose(Point.Origin, mec.Center);
            TestHelpers.AssertClose(2d, mec.Radius);
            TestHelpers.AssertEnclosesAll(mec, layout);
        }
    }

    [Fact]
    public void TwoDifferentWires_ReturnsWeightedResult()
    {
        var layout = Layout.FromWires(
            TestHelpers.Wire(-1d, 0d, 2d),
            TestHelpers.Wire(2d, 0d, 1d));

        foreach (Func<Layout, MecResult> solver in Solvers)
        {
            MecResult mec = solver(layout);
            TestHelpers.AssertPointClose(new Point(0.25d, 0d), mec.Center);
            TestHelpers.AssertClose(2.25d, mec.Radius);
            TestHelpers.AssertEnclosesAll(mec, layout);
        }
    }

    [Fact]
    public void ContainedWire_ReturnsLargerWire()
    {
        var layout = Layout.FromWires(
            TestHelpers.Wire(0d, 0d, 10d),
            TestHelpers.Wire(2d, 0d, 2d));

        foreach (Func<Layout, MecResult> solver in Solvers)
        {
            MecResult mec = solver(layout);
            TestHelpers.AssertPointClose(Point.Origin, mec.Center);
            TestHelpers.AssertClose(5d, mec.Radius);
            TestHelpers.AssertEnclosesAll(mec, layout);
        }
    }

    [Fact]
    public void ThreeEqualWires_ReturnsCircumcirclePlusRadius()
    {
        double sqrt3 = Math.Sqrt(3d);
        var layout = Layout.FromWires(
            TestHelpers.Wire(0d, 0d, 1d),
            TestHelpers.Wire(2d, 0d, 1d),
            TestHelpers.Wire(1d, sqrt3, 1d));

        var expectedCenter = new Point(1d, sqrt3 / 3d);
        double expectedRadius = 2d / sqrt3 + 0.5d;

        foreach (Func<Layout, MecResult> solver in Solvers)
        {
            MecResult mec = solver(layout);
            TestHelpers.AssertPointClose(expectedCenter, mec.Center);
            TestHelpers.AssertClose(expectedRadius, mec.Radius);
            TestHelpers.AssertEnclosesAll(mec, layout);
        }
    }

    [Fact]
    public void ConcentricWires_ReturnsLargestRadius()
    {
        var layout = Layout.FromWires(
            TestHelpers.Wire(1d, 1d, 2d),
            TestHelpers.Wire(1d, 1d, 4d),
            TestHelpers.Wire(1d, 1d, 6d));

        foreach (Func<Layout, MecResult> solver in Solvers)
        {
            MecResult mec = solver(layout);
            TestHelpers.AssertPointClose(new Point(1d, 1d), mec.Center);
            TestHelpers.AssertClose(3d, mec.Radius);
            TestHelpers.AssertEnclosesAll(mec, layout);
        }
    }

    [Fact]
    public void WeightedMec_DoesNotReduceToCentersPlusMaxRadius()
    {
        var layout = Layout.FromWires(
            TestHelpers.Wire(0d, 0d, 20d),
            TestHelpers.Wire(10d, 0d, 2d),
            TestHelpers.Wire(-10d, 0d, 2d));

        foreach (Func<Layout, MecResult> solver in Solvers)
        {
            MecResult mec = solver(layout);
            TestHelpers.AssertPointClose(Point.Origin, mec.Center);
            TestHelpers.AssertClose(11d, mec.Radius);
            TestHelpers.AssertEnclosesAll(mec, layout);
        }
    }

    [Fact]
    public void Recenter_MovesMecCenterToOrigin()
    {
        var layout = Layout.FromWires(
            TestHelpers.Wire(0d, 0d, 2d),
            TestHelpers.Wire(3d, 0d, 2d),
            TestHelpers.Wire(0d, 3d, 2d));

        MecResult mec = NumericalWeightedMecSolver.Solve(layout);
        Layout recentered = mec.Recenter(layout);
        MecResult recenteredMec = NumericalWeightedMecSolver.Solve(recentered);

        TestHelpers.AssertPointClose(Point.Origin, recenteredMec.Center);
        TestHelpers.AssertClose(mec.Radius, recenteredMec.Radius);
        TestHelpers.AssertEnclosesAll(recenteredMec, recentered);
    }

    [Fact]
    public void SupportingIndices_AreActive()
    {
        var layout = Layout.FromWires(
            TestHelpers.Wire(0d, 0d, 2d),
            TestHelpers.Wire(3d, 0d, 2d),
            TestHelpers.Wire(0d, 3d, 2d));

        foreach (Func<Layout, MecResult> solver in Solvers)
        {
            MecResult mec = solver(layout);

            Assert.NotEmpty(mec.SupportingWireIndices);
            foreach (int index in mec.SupportingWireIndices)
            {
                double value = mec.Center.DistanceTo(layout[index].Center) + layout[index].Radius;
                TestHelpers.AssertClose(mec.Radius, value);
            }
        }
    }

    [Fact]
    public void NumericalAndExact_MatchOnRandomSets()
    {
        var random = new Random(12345);

        for (int sample = 0; sample < 20; sample++)
        {
            var wires = new Wire[8];
            for (int i = 0; i < wires.Length; i++)
            {
                double x = random.NextDouble() * 20d - 10d;
                double y = random.NextDouble() * 20d - 10d;
                double diameter = 0.5d + random.NextDouble() * 4d;
                wires[i] = new Wire(new Point(x, y), diameter);
            }

            var layout = new Layout(wires);
            MecResult numerical = NumericalWeightedMecSolver.Solve(layout);
            MecResult exact = ExactWeightedMecSolver.Solve(layout);

            TestHelpers.AssertPointClose(exact.Center, numerical.Center, 1e-5);
            TestHelpers.AssertClose(exact.Radius, numerical.Radius, 1e-5);
            TestHelpers.AssertEnclosesAll(numerical, layout);
            TestHelpers.AssertEnclosesAll(exact, layout);
        }
    }

    [Fact]
    public void NumericalAndExact_MatchOnRandomSetsWithDifferentRadii()
    {
        var random = new Random(54321);

        for (int sample = 0; sample < 20; sample++)
        {
            var wires = new Wire[10];
            for (int i = 0; i < wires.Length; i++)
            {
                double x = random.NextDouble() * 200d - 100d;
                double y = random.NextDouble() * 200d - 100d;
                double diameter = 0.01d + random.NextDouble() * 99.99d;
                wires[i] = new Wire(new Point(x, y), diameter);
            }

            var layout = new Layout(wires);
            MecResult numerical = NumericalWeightedMecSolver.Solve(layout);
            MecResult exact = ExactWeightedMecSolver.Solve(layout);

            TestHelpers.AssertPointClose(exact.Center, numerical.Center, 1e-5);
            TestHelpers.AssertClose(exact.Radius, numerical.Radius, 1e-5);
            TestHelpers.AssertEnclosesAll(numerical, layout);
            TestHelpers.AssertEnclosesAll(exact, layout);
        }
    }

    [Fact]
    public void ValidateAll_ValidLayoutAndMec_ReturnsSuccess()
    {
        var layout = Layout.FromWires(
            TestHelpers.Wire(0d, 0d, 2d),
            TestHelpers.Wire(3d, 0d, 2d),
            TestHelpers.Wire(0d, 3d, 2d));
        MecResult mec = NumericalWeightedMecSolver.Solve(layout);
        var tube = new Tube(mec.Center, mec.Radius);

        Assert.True(LayoutValidator.ValidateAll(layout, tube).IsValid);
    }
}
