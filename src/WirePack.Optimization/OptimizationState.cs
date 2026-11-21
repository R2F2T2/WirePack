namespace WirePack.Optimization;

/// <summary>
/// Состояние сеанса оптимизации.
/// </summary>
public enum OptimizationState
{
    /// <summary>
    /// Сеанс создан, расчёт ещё не перешёл в активную фазу.
    /// </summary>
    Created = 0,

    /// <summary>
    /// Расчёт выполняется.
    /// </summary>
    Running = 1,

    /// <summary>
    /// Запрошена остановка, расчёт завершает текущий шаг.
    /// </summary>
    Stopping = 2,

    /// <summary>
    /// Расчёт завершён, результаты доступны.
    /// </summary>
    Completed = 3
}
