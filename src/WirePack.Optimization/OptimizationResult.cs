namespace WirePack.Optimization;

/// <summary>
/// Итоговый результат расчёта при любом способе завершения.
/// </summary>
public sealed class OptimizationResult
{
    /// <summary>
    /// Создаёт результат расчёта.
    /// </summary>
    public OptimizationResult(
        OptimizationStopReason stopReason,
        IEnumerable<LayoutVariant> variants,
        TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(variants);

        if (elapsed < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(elapsed), "Прошедшее время не может быть отрицательным.");

        StopReason = stopReason;
        Variants = Array.AsReadOnly(variants.ToArray());
        Elapsed = elapsed;
    }

    /// <summary>
    /// Причина завершения расчёта.
    /// </summary>
    public OptimizationStopReason StopReason { get; }

    /// <summary>
    /// Лучшие найденные различные варианты.
    /// </summary>
    public IReadOnlyList<LayoutVariant> Variants { get; }

    /// <summary>
    /// Время расчёта.
    /// </summary>
    public TimeSpan Elapsed { get; }
}
