using System.Diagnostics;

namespace WirePack.Core.Tests;

public class MecPerformanceTests
{
    [Fact]
    public void NumericalSolver_On100Wires_IsFastAndValid()
    {
        var random = new Random(20261121);
        var wires = new Wire[100];

        for (int i = 0; i < wires.Length; i++)
        {
            double x = random.NextDouble() * 100d;
            double y = random.NextDouble() * 100d;
            double diameter = 0.5d + random.NextDouble() * 5d;
            wires[i] = new Wire(new Point(x, y), diameter);
        }

        var layout = new Layout(wires);
        var stopwatch = Stopwatch.StartNew();
        MecResult? mec = null;

        for (int i = 0; i < 20; i++)
            mec = NumericalWeightedMecSolver.Solve(layout);

        stopwatch.Stop();

        Assert.NotNull(mec);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5d),
            $"20 расчётов MEC для 100 проводов заняли {stopwatch.Elapsed.TotalSeconds:F3} с.");

        TestHelpers.AssertEnclosesAll(mec, layout);
        Assert.True(mec.Radius >= layout.Wires.Max(wire => wire.Radius) - GeometryTolerance.Eps);
    }
}
