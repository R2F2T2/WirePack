namespace WirePack.Optimization;

/// <summary>
/// Причина завершения расчёта.
/// </summary>
public enum OptimizationStopReason
{
    /// <summary>
    /// Найдено целевое количество различных вариантов, и критерий достаточности выполнен.
    /// </summary>
    SufficientVariantsFound = 0,

    /// <summary>
    /// Расчёт остановлен по запросу пользователя.
    /// </summary>
    UserRequested = 1,

    /// <summary>
    /// Истёк аварийный лимит времени.
    /// </summary>
    TimeLimitReached = 2
}
