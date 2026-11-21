using System.Diagnostics;
using WirePack.Core.Geometry;

namespace WirePack.Optimization.Tests;

/// <summary>
/// Тесты производительности MEC.
/// Задача 4.11: проверка, что MEC не является узким местом.
/// </summary>
public class MecPerformanceTests
{
    [Fact]
    public void Solve_OnHundredWires_IsUnderOneHundredMilliseconds()
    {
        var layout = new Layout(TestWireSets.HundredWires);

        var stopwatch = Stopwatch.StartNew();

        // Прогрев
        NumericalWeightedMecSolver.Solve(layout);

        const int runs = 10;
        long totalTicks = 0;

        for (int i = 0; i < runs; i++)
        {
            var sw = Stopwatch.StartNew();
            MecResult result = NumericalWeightedMecSolver.Solve(layout);
            sw.Stop();
            totalTicks += sw.ElapsedTicks;

            Assert.True(double.IsFinite(result.Radius) && result.Radius > 0d,
                "MEC вернул некорректный радиус.");
            Assert.True(result.Center.IsFinite,
                "MEC вернул некорректный центр.");
        }

        var average = TimeSpan.FromTicks(totalTicks / runs);
        Assert.True(average.TotalMilliseconds < 200d,
            $"Среднее время MEC на 100 проводах: {average.TotalMilliseconds:F2} мс (порог: 200 мс).");
    }

    [Fact]
    public void Solve_OnUniformWires_ReturnsReasonableRadius()
    {
        // 10 проводов диаметром 2 мм должны дать предсказуемую MEC
        var layout = new Layout(TestWireSets.UniformWires(10, 2d));

        MecResult mec = NumericalWeightedMecSolver.Solve(layout);

        Assert.True(mec.Radius > 0d && mec.Radius < 20d,
            $"Некорректный радиус MEC: {mec.Radius}");
    }

    [Fact]
    public void Solve_OnSingleWire_ReturnsWireCenter()
    {
        var layout = Layout.FromWires(new Wire(new Point(5d, -3d), 4d));

        MecResult mec = NumericalWeightedMecSolver.Solve(layout);

        Assert.Equal(new Point(5d, -3d), mec.Center);
        Assert.Equal(2d, mec.Radius, 9);
    }

    [Fact]
    public void Solve_OnTwoWires_ReturnsCorrectMec()
    {
        var layout = Layout.FromWires(
            new Wire(new Point(0d, 0d), 2d),
            new Wire(new Point(4d, 0d), 2d));

        MecResult mec = NumericalWeightedMecSolver.Solve(layout);

        // Два провода радиусом 1, расстояние между центрами 4
        // MEC: центр (2,0), радиус = max(|0-2|+1, |2-0|+1) = 3
        Assert.Equal(new Point(2d, 0d), mec.Center);
        Assert.Equal(3d, mec.Radius, 6);
    }

    [Fact]
    public void Solve_OnHundredWires_ProducesDeterministicResult()
    {
        var layout = new Layout(TestWireSets.HundredWires);

        MecResult first = NumericalWeightedMecSolver.Solve(layout);
        MecResult second = NumericalWeightedMecSolver.Solve(layout);

        Assert.Equal(first.Center, second.Center);
        Assert.Equal(first.Radius, second.Radius, 9);
    }
}