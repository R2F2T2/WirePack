namespace WirePack.Optimization;

/// <summary>
/// Фаза выполнения оптимизации.
/// </summary>
public enum OptimizationPhase
{
    /// <summary>
    /// Фаза не определена.
    /// </summary>
    None = 0,

    /// <summary>
    /// Построение начальной укладки.
    /// </summary>
    Placement = 1,

    /// <summary>
    /// Сжатие пучка.
    /// </summary>
    Compression = 2,

    /// <summary>
    /// Расчёт завершён.
    /// </summary>
    Completed = 3
}
