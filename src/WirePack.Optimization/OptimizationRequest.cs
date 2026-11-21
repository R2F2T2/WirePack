using WirePack.Core.Geometry;

namespace WirePack.Optimization;

/// <summary>
/// Исходные данные и параметры запуска оптимизации.
/// </summary>
/// <remarks>
/// Параметры алгоритма не настраиваются пользователем интерфейса. Значения по умолчанию
/// соответствуют требованиям: фиксированный seed, бюджет 60 секунд и до пяти вариантов.
/// </remarks>
public sealed class OptimizationRequest
{
    /// <summary>
    /// Количество вариантов, которое стремится найти оптимизатор.
    /// </summary>
    public const int DefaultTargetVariantCount = 5;

    /// <summary>
    /// Фиксированный seed по умолчанию.
    /// </summary>
    public const int DefaultSeed = 0;

    private static readonly TimeSpan DefaultTimeLimitValue = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Создаёт запрос на оптимизацию.
    /// </summary>
    public OptimizationRequest(
        IEnumerable<Wire> wires,
        int seed = DefaultSeed,
        TimeSpan? timeLimit = null,
        int targetVariantCount = DefaultTargetVariantCount,
        bool visualizeSteps = false)
    {
        ArgumentNullException.ThrowIfNull(wires);

        Wire[] wireArray = wires.ToArray();
        if (wireArray.Length == 0)
            throw new ArgumentException("Набор проводов не должен быть пустым.", nameof(wires));

        Wires = Array.AsReadOnly(wireArray);

        if (targetVariantCount < 1 || targetVariantCount > DefaultTargetVariantCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetVariantCount),
                $"Количество вариантов должно быть от 1 до {DefaultTargetVariantCount}.");
        }

        TimeSpan limit = timeLimit ?? DefaultTimeLimitValue;
        if (limit <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeLimit), "Лимит времени должен быть положительным.");

        Seed = seed;
        TimeLimit = limit;
        TargetVariantCount = targetVariantCount;
        VisualizeSteps = visualizeSteps;
    }

    /// <summary>
    /// Провода, для которых ищется компактная укладка.
    /// </summary>
    public IReadOnlyList<Wire> Wires { get; }

    /// <summary>
    /// Seed генератора случайных чисел, обеспечивающий воспроизводимость.
    /// </summary>
    public int Seed { get; }

    /// <summary>
    /// Аварийный лимит времени расчёта.
    /// </summary>
    public TimeSpan TimeLimit { get; }

    /// <summary>
    /// Целевое количество геометрически различных вариантов.
    /// </summary>
    public int TargetVariantCount { get; }

    /// <summary>
    /// Флаг пошаговой визуализации (с задержкой при размещении проводов).
    /// </summary>
    public bool VisualizeSteps { get; }
}
