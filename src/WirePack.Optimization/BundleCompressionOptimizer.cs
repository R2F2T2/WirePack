using System.Diagnostics;
using System.Linq;
using WirePack.Core.Geometry;

namespace WirePack.Optimization;

/// <summary>
/// Реализация оптимизатора сжатия пучка с квазистатической релаксацией.
/// </summary>
public sealed class BundleCompressionOptimizer : IBundleCompressionOptimizer
{
    /// <inheritdoc />
    public IOptimizationSession Start(
        OptimizationRequest request,
        IProgress<OptimizationProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new OptimizationSessionImplementation(request, progress);
    }

    private sealed class OptimizationSessionImplementation : IOptimizationSession
    {
        private const int MaxRestarts = 100;
        private const int EarlyTerminationThresholdN = 30;
        private readonly CancellationTokenSource _cts = new();
        private readonly TaskCompletionSource<OptimizationResult> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _lock = new();

        private OptimizationProgress _currentProgress;
        private List<LayoutVariant> _variants = new();
        private double? _bestDiameter;
        private int _iterationsWithoutImprovement;

        public OptimizationSessionImplementation(
            OptimizationRequest request,
            IProgress<OptimizationProgress>? progress)
        {
            Request = request;
            // Инициализируем прогресс текущим состоянием
            _currentProgress = new OptimizationProgress(
                OptimizationState.Running,
                TimeSpan.Zero,
                iteration: 0,
                restartCount: 0,
                variantCount: 0,
                bestDiameter: null);

            Completion = _tcs.Task;

            Task.Run(() => RunOptimization(progress));
        }

        public OptimizationRequest Request { get; }
        public Task<OptimizationResult> Completion { get; }

        public OptimizationSnapshot GetSnapshot()
        {
            lock (_lock)
            {
                return new OptimizationSnapshot(_currentProgress, _variants);
            }
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        private void RunOptimization(IProgress<OptimizationProgress>? progress)
        {
            var stopwatch = Stopwatch.StartNew();
            var equalityComparer = new LayoutEqualityComparer();
            int totalIterations = 0;
            int restartCount = 0;

            try
            {
                for (int r = 0; r < MaxRestarts; r++)
                {
                    if (_cts.Token.IsCancellationRequested || stopwatch.Elapsed >= Request.TimeLimit)
                        break;

                    restartCount = r + 1;
                    int seed = Request.Seed + r;
                    var generator = new InitialLayoutGenerator(seed);

                    Layout initialLayout;
                    try
                    {
                        Action<Layout, Tube?>? onWirePlaced = Request.VisualizeSteps
                            ? (partialLayout, partialTube) =>
                            {
                                if (_cts.Token.IsCancellationRequested) return;
                                lock (_lock)
                                {
                                    _currentProgress = new OptimizationProgress(
                                        OptimizationState.Running,
                                        stopwatch.Elapsed,
                                        totalIterations,
                                        restartCount,
                                        _variants.Count,
                                        _bestDiameter,
                                        partialLayout,
                                        partialTube);

                                    progress?.Report(_currentProgress);
                                }
                                Thread.Sleep(200);
                            }
                            : null;

                        initialLayout = generator.Generate(Request.Wires, onWirePlaced);
                    }
                    catch
                    {
                        continue;
                    }

                    // Шаг 3.3.1: Вычисляем MEC и центрируем укладку
                    var initialMec = NumericalWeightedMecSolver.Solve(initialLayout);
                    // Центрируем: перемещаем центр MEC в origine
                    initialLayout = initialLayout.Translate(-initialMec.Center.X, -initialMec.Center.Y);
                    // Пересчитываем MEC после центрирования
                    initialMec = NumericalWeightedMecSolver.Solve(initialLayout);
                    var initialTube = new Tube(initialMec.Center, initialMec.Radius);

                    lock (_lock)
                    {
                        _currentProgress = new OptimizationProgress(
                            OptimizationState.Running,
                            stopwatch.Elapsed,
                            totalIterations,
                            restartCount,
                            _variants.Count,
                            _bestDiameter,
                            initialLayout,
                            initialTube);

                        progress?.Report(_currentProgress);
                    }

                    double currentRadius = initialMec.Radius;
                    Layout bestLayoutInRestart = initialLayout;
                    double bestRadiusInRestart = currentRadius;

                    double delta = currentRadius * 0.05d;
                    if (delta < 0.01d) delta = 0.01d;

                    bool improvedInRestart = false;

                    for (int iter = 0; iter < 100; iter++)
                    {
                        if (_cts.Token.IsCancellationRequested || stopwatch.Elapsed >= Request.TimeLimit)
                            break;

                        totalIterations++;

                        double targetRadius = bestRadiusInRestart - delta;
                        if (targetRadius <= 0d)
                            break;

                        // Шаг 3.3.3: Выполняем релаксацию
                        var (relaxedLayout, success) = RelaxationEngine.Relax(bestLayoutInRestart, targetRadius);

                        if (success)
                        {
                            // Шаг 3.3.4: Пересчитываем MEC по фактическим координатам
                            var mec = NumericalWeightedMecSolver.Solve(relaxedLayout);
                            // Шаг 3.3.5: Перецентрируем по MEC (в начале координат)
                            var centeredLayout = relaxedLayout.Translate(-mec.Center.X, -mec.Center.Y);
                            var mecCentered = NumericalWeightedMecSolver.Solve(centeredLayout);
                            var tube = new Tube(mecCentered.Center, mecCentered.Radius);

                            // Шаг 3.7: Независимая проверка через Core
                            var validation = LayoutValidator.ValidateAll(centeredLayout, tube);
                            if (validation.IsValid)
                            {
                                double newDiameter = tube.InnerDiameter;
                                bool diameterImproved = false;

                                if (newDiameter < bestRadiusInRestart * 2d - GeometryTolerance.Eps)
                                {
                                    bestLayoutInRestart = centeredLayout;
                                    bestRadiusInRestart = mecCentered.Radius;
                                    improvedInRestart = true;
                                    diameterImproved = true;
                                }

                                var variant = new LayoutVariant(centeredLayout);

                                lock (_lock)
                                {
                                    bool added = AddOrUpdateVariant(variant, equalityComparer);
                                    if (added || _bestDiameter == null || variant.Diameter < _bestDiameter.Value)
                                    {
                                        if (diameterImproved)
                                        {
                                            _bestDiameter = variant.Diameter;
                                            _iterationsWithoutImprovement = 0;
                                        }
                                        else
                                        {
                                            _iterationsWithoutImprovement++;
                                        }
                                    }
                                    else
                                    {
                                        _iterationsWithoutImprovement++;
                                    }

                                     _currentProgress = new OptimizationProgress(
                                         OptimizationState.Running,
                                         stopwatch.Elapsed,
                                         totalIterations,
                                         restartCount,
                                         _variants.Count,
                                         _bestDiameter,
                                         centeredLayout,
                                         tube);

                                    progress?.Report(_currentProgress);
                                }

                                // Увеличиваем шаг сжатия при успехе
                                if (diameterImproved) delta *= 1.2d;
                                else delta *= 0.5d;
                            }
                            else
                            {
                                delta *= 0.5d;
                                if (delta < 1e-4d) break;
                            }
                        }
                        else
                        {
                            delta *= 0.5d;
                            if (delta < 1e-4d) break;
                        }
                    }

                    if (!improvedInRestart)
                    {
                        lock (_lock)
                        {
                            _iterationsWithoutImprovement++;
                        }
                    }

                    // Проверка критерия раннего завершения (3.6.4)
                    lock (_lock)
                    {
                        if (_variants.Count >= Request.TargetVariantCount &&
                            _iterationsWithoutImprovement >= EarlyTerminationThresholdN)
                        {
                            break;
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем исключения, обработка идет в блоке завершения
            }

            stopwatch.Stop();

            // Определяем причину остановки
            OptimizationStopReason stopReason;
            if (_cts.IsCancellationRequested)
            {
                stopReason = OptimizationStopReason.UserRequested;
            }
            else if (stopwatch.Elapsed >= Request.TimeLimit)
            {
                stopReason = OptimizationStopReason.TimeLimitReached;
            }
            else
            {
                // Проверяем, были ли найдены варианты и критерий достаточности
                bool sufficientVariants = _variants.Count >= Request.TargetVariantCount;
                stopReason = sufficientVariants ? OptimizationStopReason.SufficientVariantsFound : OptimizationStopReason.UserRequested;
            }

            OptimizationResult result;
            lock (_lock)
            {
                _currentProgress = new OptimizationProgress(
                    OptimizationState.Completed,
                    stopwatch.Elapsed,
                    totalIterations,
                    restartCount,
                    _variants.Count,
                    _bestDiameter);

                result = new OptimizationResult(
                    stopReason,
                    _variants,
                    stopwatch.Elapsed);
            }

            progress?.Report(_currentProgress);
            _tcs.TrySetResult(result);
        }

        private bool AddOrUpdateVariant(LayoutVariant variant, LayoutEqualityComparer comparer)
        {
            // Проверяем, есть ли уже эквивалентный вариант
            int existingIndex = -1;
            for (int i = 0; i < _variants.Count; i++)
            {
                if (comparer.Equals(_variants[i].Layout, variant.Layout))
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                // Если новый вариант лучше (меньше диаметр), заменяем
                if (variant.Diameter < _variants[existingIndex].Diameter - GeometryTolerance.Eps)
                {
                    _variants[existingIndex] = variant;
                    SortAndTrimVariants();
                    return true;
                }
                return false;
            }

            if (_variants.Count < Request.TargetVariantCount)
            {
                _variants.Add(variant);
                SortAndTrimVariants();
                return true;
            }

            // Если список полон, проверяем, лучше ли новый вариант самого худшего в топ-5
            double maxDiameter = _variants.Count > 0 ? _variants.Max(v => v.Diameter) : double.MaxValue;
            if (variant.Diameter < maxDiameter - GeometryTolerance.Eps)
            {
                _variants.Add(variant);
                SortAndTrimVariants();
                return true;
            }

            return false;
        }

        private void SortAndTrimVariants()
        {
            if (_variants.Count == 0) return;
            _variants = _variants
                .OrderBy(v => v.Diameter)
                .Take(Request.TargetVariantCount)
                .ToList();
        }
    }
}