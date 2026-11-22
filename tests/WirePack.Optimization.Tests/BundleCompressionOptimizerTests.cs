using System.Diagnostics;
using WirePack.Core.Geometry;

namespace WirePack.Optimization.Tests;

/// <summary>
/// Интеграционные тесты оптимизатора BundleCompressionOptimizer.
/// Покрывают задачи 4.3–4.10 этапа 4.
/// </summary>
public class BundleCompressionOptimizerTests
{
    private static readonly BundleCompressionOptimizer Optimizer = new();

    [Fact]
    public async Task Start_ReturnsGeometricallyValidVariants()
    {
        // 4.3: Проверка геометрической допустимости — все решения проходят Core
        var request = new OptimizationRequest(
            TestWireSets.ThreeWires,
            timeLimit: TimeSpan.FromSeconds(10));

        var session = Optimizer.Start(request);
        OptimizationResult result = await session.Completion;

        Assert.NotEmpty(result.Variants);

        foreach (LayoutVariant variant in result.Variants)
        {
            var validation = LayoutValidator.ValidateAll(variant.Layout, variant.Tube);
            Assert.True(validation.IsValid,
                $"Вариант не прошёл проверку: {string.Join("; ", validation.Issues.Select(i => i.Message))}");
            Assert.InRange(variant.FillFactor, 0d, 1d);
        }
    }

    [Fact]
    public void LayoutVariant_FillFactor_SingleWire_IsOne()
    {
        var variant = new LayoutVariant(Layout.FromWires(new Wire(Point.Origin, 2d)));

        Assert.Equal(1d, variant.FillFactor, 9);
    }

    [Fact]
    public void LayoutVariant_FillFactor_TwoEqualWires_IsHalf()
    {
        var variant = new LayoutVariant(Layout.FromWires(
            new Wire(new Point(-1d, 0d), 2d),
            new Wire(new Point(1d, 0d), 2d)));

        Assert.Equal(0.5d, variant.FillFactor, 9);
    }

    [Fact]
    public async Task Start_ComputesFinalDiameterViaMec()
    {
        // 4.4: Финальный диаметр вычисляется через MEC по фактическим координатам
        var request = new OptimizationRequest(
            TestWireSets.MixedWires,
            timeLimit: TimeSpan.FromSeconds(10));

        var session = Optimizer.Start(request);
        OptimizationResult result = await session.Completion;

        Assert.NotEmpty(result.Variants);

        foreach (LayoutVariant variant in result.Variants)
        {
            // Пересчитываем MEC через Core независимо
            MecResult recalculatedMec = NumericalWeightedMecSolver.Solve(variant.Layout);
            double expectedDiameter = recalculatedMec.Diameter;

            Assert.Equal(expectedDiameter, variant.Diameter, 6);
        }
    }

    [Fact]
    public async Task Start_ReturnsUpToFiveVariants()
    {
        // 4.5: Количество вариантов — до пяти различных
        var request = new OptimizationRequest(
            TestWireSets.HundredWires,
            timeLimit: TimeSpan.FromSeconds(15));

        var session = Optimizer.Start(request);
        OptimizationResult result = await session.Completion;

        Assert.InRange(result.Variants.Count, 1, 5);

        // Проверяем, что варианты действительно различны
        var comparer = new LayoutEqualityComparer();
        for (int i = 0; i < result.Variants.Count; i++)
        {
            for (int j = i + 1; j < result.Variants.Count; j++)
            {
                Assert.False(comparer.Equals(result.Variants[i].Layout, result.Variants[j].Layout),
                    $"Варианты {i} и {j} эквивалентны.");
            }
        }
    }

    [Fact]
    public async Task Start_RespectsTimeLimit()
    {
        // 4.7: Таймаут — автоматическая остановка по истечении лимита
        var request = new OptimizationRequest(
            TestWireSets.HundredWires,
            timeLimit: TimeSpan.FromMilliseconds(100));

        var session = Optimizer.Start(request);
        OptimizationResult result = await session.Completion;

        Assert.Equal(OptimizationStopReason.TimeLimitReached, result.StopReason);
        Assert.True(result.Elapsed <= request.TimeLimit + TimeSpan.FromMilliseconds(500),
            $"Время {result.Elapsed} превысило лимит {request.TimeLimit}");
        Assert.NotNull(result.Variants);
    }

    [Fact]
    public async Task Start_HandlesEarlyTermination()
    {
        // 4.8: Раннее завершение — с одним целевым вариантом завершается быстро
        var request = new OptimizationRequest(
            TestWireSets.ThreeWires,
            targetVariantCount: 1,
            timeLimit: TimeSpan.FromSeconds(10));

        var session = Optimizer.Start(request);
        OptimizationResult result = await session.Completion;

        Assert.NotEmpty(result.Variants);
        Assert.Equal(OptimizationStopReason.SufficientVariantsFound, result.StopReason);
    }

    [Fact]
    public async Task Start_ManualStopPreservesResults()
    {
        // 4.9: Ручная остановка — результаты сохраняются
        var request = new OptimizationRequest(
            TestWireSets.HundredWires,
            timeLimit: TimeSpan.FromSeconds(30));

        var session = Optimizer.Start(request);

        // Ждём немного, чтобы оптимизатор успел найти хоть что-то
        await Task.Delay(500);
        session.Stop();

        OptimizationResult result = await session.Completion;

        Assert.Equal(OptimizationStopReason.UserRequested, result.StopReason);
        Assert.NotNull(result.Variants);
        Assert.True(result.Elapsed > TimeSpan.Zero);
    }

    [Fact]
    public async Task Start_IsDeterministicWithSameSeed()
    {
        // 4.10: Воспроизводимость — одинаковый вход и seed дают одинаковый результат
        var requestBase = new OptimizationRequest(
            TestWireSets.MixedWires,
            seed: 42,
            timeLimit: TimeSpan.FromSeconds(10));

        var session1 = Optimizer.Start(requestBase);
        OptimizationResult result1 = await session1.Completion;

        var session2 = Optimizer.Start(requestBase);
        OptimizationResult result2 = await session2.Completion;

        Assert.Equal(result1.Variants.Count, result2.Variants.Count);
        Assert.Equal(result1.StopReason, result2.StopReason);

        var comparer = new LayoutEqualityComparer();
        for (int i = 0; i < result1.Variants.Count; i++)
        {
            Assert.True(comparer.Equals(result1.Variants[i].Layout, result2.Variants[i].Layout),
                $"Вариант {i} различается между запусками.");
        }
    }

    [Fact]
    public async Task Start_CompletesHundredWiresWithinSixtySeconds()
    {
        // 4.6: Время на 100 проводах — не более 60 секунд
        var request = new OptimizationRequest(
            TestWireSets.HundredWires,
            timeLimit: TimeSpan.FromSeconds(60));

        var stopwatch = Stopwatch.StartNew();
        var session = Optimizer.Start(request);
        OptimizationResult result = await session.Completion;
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed <= TimeSpan.FromSeconds(60),
            $"Расчёт на 100 проводах занял {stopwatch.Elapsed.TotalSeconds:F1} с, что превышает 60 с.");

        Assert.NotEmpty(result.Variants);
        foreach (LayoutVariant variant in result.Variants)
        {
            var validation = LayoutValidator.ValidateAll(variant.Layout, variant.Tube);
            Assert.True(validation.IsValid,
                $"Вариант не прошёл проверку: {string.Join("; ", validation.Issues.Select(i => i.Message))}");
        }
    }

    [Fact]
    public async Task Start_StopIsIdempotent()
    {
        // Повторный вызов Stop не изменяет результат
        var request = new OptimizationRequest(
            TestWireSets.ThreeWires,
            timeLimit: TimeSpan.FromSeconds(5));

        var session = Optimizer.Start(request);

        session.Stop();
        session.Stop(); // второй вызов

        OptimizationResult result = await session.Completion;

        Assert.Equal(OptimizationStopReason.UserRequested, result.StopReason);
    }

    [Fact]
    public async Task Start_ThrowsOnNullRequest()
    {
        Assert.Throws<ArgumentNullException>(() => Optimizer.Start(null!));
    }

    [Fact]
    public async Task Start_ReportsPlacementAndCompletedPhases()
    {
        var phases = new List<OptimizationPhase>();
        var gate = new object();
        var progress = new InlineProgress<OptimizationProgress>(p =>
        {
            lock (gate)
                phases.Add(p.Phase);
        });

        var request = new OptimizationRequest(
            TestWireSets.ThreeWires,
            targetVariantCount: 1,
            timeLimit: TimeSpan.FromSeconds(1));

        var session = Optimizer.Start(request, progress);
        await session.Completion;

        lock (gate)
        {
            Assert.Contains(OptimizationPhase.Placement, phases);
            Assert.Contains(OptimizationPhase.Completed, phases);
        }
    }

    private sealed class InlineProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;

        public InlineProgress(Action<T> handler) => _handler = handler;

        public void Report(T value) => _handler(value);
    }
}