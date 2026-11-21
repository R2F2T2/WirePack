using WirePack.Core.Geometry;

namespace WirePack.Optimization.Tests;

public class OptimizationContractTests
{
    [Fact]
    public void Request_UsesRequiredDefaults()
    {
        var request = new OptimizationRequest(new[]
        {
            new Wire(Point.Origin, 2d)
        });

        Assert.Equal(OptimizationRequest.DefaultSeed, request.Seed);
        Assert.Equal(TimeSpan.FromSeconds(60), request.TimeLimit);
        Assert.Equal(OptimizationRequest.DefaultTargetVariantCount, request.TargetVariantCount);
        Assert.Single(request.Wires);
    }

    [Fact]
    public void Request_CopiesInputWires()
    {
        var wires = new List<Wire>
        {
            new(Point.Origin, 2d)
        };

        var request = new OptimizationRequest(wires);
        wires.Clear();

        Assert.Single(request.Wires);
    }

    [Fact]
    public void Request_RejectsEmptyWireSet()
    {
        Assert.Throws<ArgumentException>(() =>
            new OptimizationRequest(Array.Empty<Wire>()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Request_RejectsInvalidTargetVariantCount(int targetVariantCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OptimizationRequest(
                new[] { new Wire(Point.Origin, 2d) },
                targetVariantCount: targetVariantCount));
    }

    [Fact]
    public void Request_RejectsNonPositiveTimeLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OptimizationRequest(
                new[] { new Wire(Point.Origin, 2d) },
                timeLimit: TimeSpan.Zero));
    }

    [Fact]
    public void LayoutVariant_ComputesWeightedMecDiameter()
    {
        var layout = Layout.FromWires(
            new Wire(Point.Origin, 2d),
            new Wire(new Point(3d, 0d), 4d));

        var variant = new LayoutVariant(layout);

        Assert.Equal(3d, variant.Mec.Radius, 9);
        Assert.Equal(new Point(2d, 0d), variant.Mec.Center);
        Assert.Equal(6d, variant.Diameter, 9);
    }

    [Fact]
    public void LayoutVariant_RecalculatesDiameterFromCoordinates()
    {
        var layout = Layout.FromWires(
            new Wire(new Point(-10d, 5d), 2d),
            new Wire(new Point(-8d, 5d), 2d));

        var variant = new LayoutVariant(layout);

        Assert.Equal(4d, variant.Diameter, 9);
        Assert.Equal(new Point(-9d, 5d), variant.Mec.Center);
    }

    [Fact]
    public void Snapshot_CopiesVariants()
    {
        var variants = new List<LayoutVariant>
        {
            new(Layout.FromWires(new Wire(Point.Origin, 2d)))
        };

        var snapshot = new OptimizationSnapshot(OptimizationProgress.Initial, variants);
        variants.Clear();

        Assert.Single(snapshot.Variants);
        Assert.Equal(OptimizationState.Created, snapshot.Progress.State);
    }

    [Fact]
    public void Result_PreservesStopReasonAndVariants()
    {
        var variants = new List<LayoutVariant>
        {
            new(Layout.FromWires(new Wire(Point.Origin, 2d)))
        };

        var result = new OptimizationResult(
            OptimizationStopReason.UserRequested,
            variants,
            TimeSpan.FromSeconds(1));
        variants.Clear();

        Assert.Equal(OptimizationStopReason.UserRequested, result.StopReason);
        Assert.Single(result.Variants);
        Assert.Equal(TimeSpan.FromSeconds(1), result.Elapsed);
    }

    [Fact]
    public void Progress_RejectsInvalidBestDiameter()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OptimizationProgress(
                OptimizationState.Running,
                TimeSpan.Zero,
                iteration: 1,
                restartCount: 0,
                variantCount: 1,
                bestDiameter: 0d));
    }
}
