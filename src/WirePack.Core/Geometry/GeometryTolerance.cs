namespace WirePack.Core.Geometry;

/// <summary>
/// Единое правило сравнения геометрических величин.
/// </summary>
public static class GeometryTolerance
{
    /// <summary>
    /// Численный допуск в миллиметрах, учитывающий погрешность вычислений с <see cref="double"/>.
    /// </summary>
    public const double Eps = 1e-9;

    /// <summary>
    /// Проверяет, что величина практически равна нулю.
    /// </summary>
    public static bool IsZero(double value) => Math.Abs(value) <= Eps;

    /// <summary>
    /// Проверяет, что две величины практически равны.
    /// </summary>
    public static bool AreClose(double first, double second) => Math.Abs(first - second) <= Eps;

    /// <summary>
    /// Проверяет, что значение не превосходит предел с учётом допуска.
    /// </summary>
    public static bool IsLessOrEqual(double value, double limit) => value <= limit + Eps;

    /// <summary>
    /// Проверяет, что значение не меньше предела с учётом допуска.
    /// </summary>
    public static bool IsGreaterOrEqual(double value, double limit) => value >= limit - Eps;

    /// <summary>
    /// Проверяет, что значение строго меньше предела с учётом допуска.
    /// </summary>
    public static bool IsLess(double value, double limit) => value < limit - Eps;

    /// <summary>
    /// Проверяет, что значение строго больше предела с учётом допуска.
    /// </summary>
    public static bool IsGreater(double value, double limit) => value > limit + Eps;
}
