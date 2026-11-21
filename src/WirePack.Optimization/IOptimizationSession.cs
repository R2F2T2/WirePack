namespace WirePack.Optimization;

/// <summary>
/// Запущенный сеанс оптимизации.
/// </summary>
/// <remarks>
/// Реализация должна быть безопасной для чтения снимка из другого потока, например из UI.
/// Остановка идемпотентна: повторный вызов <see cref="Stop"/> не изменяет результат.
/// </remarks>
public interface IOptimizationSession
{
    /// <summary>
    /// Запрос, с которым запущен сеанс.
    /// </summary>
    OptimizationRequest Request { get; }

    /// <summary>
    /// Завершается после остановки, таймаута или выполнения критерия достаточности.
    /// Всегда возвращает лучшие найденные результаты, не выбрасывая исключение при остановке.
    /// </summary>
    Task<OptimizationResult> Completion { get; }

    /// <summary>
    /// Возвращает текущий прогресс и лучшие результаты в любой момент расчёта.
    /// </summary>
    OptimizationSnapshot GetSnapshot();

    /// <summary>
    /// Запрашивает корректную остановку расчёта с сохранением найденных результатов.
    /// </summary>
    void Stop();
}
