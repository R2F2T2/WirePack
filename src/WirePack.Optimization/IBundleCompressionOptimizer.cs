namespace WirePack.Optimization;

/// <summary>
/// Контракт оптимизатора сжатия пучка.
/// </summary>
public interface IBundleCompressionOptimizer
{
    /// <summary>
    /// Запускает расчёт асинхронно относительно вызывающего потока и сразу возвращает сеанс.
    /// </summary>
    /// <param name="request">Исходные данные и параметры расчёта.</param>
    /// <param name="progress">
    /// Необязательный приёмник обновлений прогресса. Не должен блокировать расчёт.
    /// </param>
    IOptimizationSession Start(
        OptimizationRequest request,
        IProgress<OptimizationProgress>? progress = null);
}
