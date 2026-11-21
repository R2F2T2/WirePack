namespace WirePack.Optimization;

/// <summary>
/// Согласованный снимок прогресса и лучших найденных вариантов на текущий момент.
/// </summary>
public sealed class OptimizationSnapshot
{
    /// <summary>
    /// Создаёт снимок состояния расчёта.
    /// </summary>
    public OptimizationSnapshot(OptimizationProgress progress, IEnumerable<LayoutVariant> variants)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(variants);

        Progress = progress;
        Variants = Array.AsReadOnly(variants.ToArray());
    }

    /// <summary>
    /// Прогресс и состояние расчёта.
    /// </summary>
    public OptimizationProgress Progress { get; }

    /// <summary>
    /// Лучшие найденные различные варианты на момент снимка.
    /// </summary>
    public IReadOnlyList<LayoutVariant> Variants { get; }
}
