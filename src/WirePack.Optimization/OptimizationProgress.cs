namespace WirePack.Optimization;

/// <summary>
/// Текущий прогресс расчёта для отображения в интерфейсе.
/// </summary>
public sealed record OptimizationProgress
{
    /// <summary>
    /// Начальный прогресс до запуска расчёта.
    /// </summary>
    public static OptimizationProgress Initial { get; } = new(
        OptimizationState.Created,
        TimeSpan.Zero,
        iteration: 0,
        restartCount: 0,
        variantCount: 0,
        bestDiameter: null,
        currentLayout: null,
        currentTube: null);

    /// <summary>
    /// Создаёт снимок прогресса.
    /// </summary>
    public OptimizationProgress(
        OptimizationState state,
        TimeSpan elapsed,
        int iteration,
        int restartCount,
        int variantCount,
        double? bestDiameter,
        WirePack.Core.Geometry.Layout? currentLayout = null,
        WirePack.Core.Geometry.Tube? currentTube = null,
        OptimizationPhase phase = OptimizationPhase.None)
    {
        if (elapsed < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(elapsed), "Прошедшее время не может быть отрицательным.");

        if (iteration < 0)
            throw new ArgumentOutOfRangeException(nameof(iteration), "Номер итерации не может быть отрицательным.");

        if (restartCount < 0)
            throw new ArgumentOutOfRangeException(nameof(restartCount), "Количество перезапусков не может быть отрицательным.");

        if (variantCount < 0)
            throw new ArgumentOutOfRangeException(nameof(variantCount), "Количество вариантов не может быть отрицательным.");

        if (bestDiameter is double diameter && (!double.IsFinite(diameter) || diameter <= 0d))
        {
            throw new ArgumentOutOfRangeException(
                nameof(bestDiameter),
                "Лучший диаметр должен быть положительным конечным числом или отсутствовать.");
        }

        State = state;
        Elapsed = elapsed;
        Iteration = iteration;
        RestartCount = restartCount;
        VariantCount = variantCount;
        BestDiameter = bestDiameter;
        CurrentLayout = currentLayout;
        CurrentTube = currentTube;
        Phase = phase;
    }

    /// <summary>
    /// Текущее состояние расчёта.
    /// </summary>
    public OptimizationState State { get; }

    /// <summary>
    /// Время, прошедшее с момента запуска.
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// Номер текущей итерации алгоритма.
    /// </summary>
    public int Iteration { get; }

    /// <summary>
    /// Количество выполненных перезапусков.
    /// </summary>
    public int RestartCount { get; }

    /// <summary>
    /// Количество найденных различных вариантов.
    /// </summary>
    public int VariantCount { get; }

    /// <summary>
    /// Лучший найденный диаметр или <see langword="null"/>, если вариантов ещё нет.
    /// </summary>
    public double? BestDiameter { get; }

    /// <summary>
    /// Текущая компоновка на итерации (может быть null).
    /// </summary>
    public WirePack.Core.Geometry.Layout? CurrentLayout { get; }

    /// <summary>
    /// Текущая защитная трубка (MEC) на итерации (может быть null).
    /// </summary>
    public WirePack.Core.Geometry.Tube? CurrentTube { get; }

    /// <summary>
    /// Текущая фаза расчёта.
    /// </summary>
    public OptimizationPhase Phase { get; }
}
