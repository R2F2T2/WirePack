namespace WirePack.Core.Geometry;

/// <summary>
/// Набор проводов с координатами центров.
/// </summary>
public sealed class Layout
{
    private readonly Wire[] _wires;

    public Layout(IEnumerable<Wire> wires)
    {
        ArgumentNullException.ThrowIfNull(wires);
        _wires = wires.ToArray();
    }

    /// <summary>
    /// Пустая компоновка.
    /// </summary>
    public static Layout Empty { get; } = new(Array.Empty<Wire>());

    /// <summary>
    /// Создаёт компоновку из переданных проводов.
    /// </summary>
    public static Layout FromWires(params Wire[] wires) => new(wires);

    /// <summary>
    /// Количество проводов.
    /// </summary>
    public int Count => _wires.Length;

    /// <summary>
    /// Провода компоновки.
    /// </summary>
    public IReadOnlyList<Wire> Wires => _wires;

    /// <summary>
    /// Возвращает провод по индексу.
    /// </summary>
    public Wire this[int index] => _wires[index];

    /// <summary>
    /// Возвращает компоновку, перенесённую на заданные приращения координат.
    /// </summary>
    public Layout Translate(double dx, double dy)
    {
        if (!double.IsFinite(dx) || !double.IsFinite(dy))
            throw new ArgumentOutOfRangeException(nameof(dx), "Смещение должно быть конечным.");

        var translated = new Wire[_wires.Length];
        for (int i = 0; i < _wires.Length; i++)
            translated[i] = _wires[i].MoveTo(_wires[i].Center.Translate(dx, dy));

        return new Layout(translated);
    }

    /// <summary>
    /// Возвращает компоновку, перенесённую на заданный вектор.
    /// </summary>
    public Layout Translate(Point offset) => Translate(offset.X, offset.Y);

    /// <summary>
    /// Возвращает центры всех проводов.
    /// </summary>
    public Point[] GetCenters()
    {
        var centers = new Point[_wires.Length];
        for (int i = 0; i < _wires.Length; i++)
            centers[i] = _wires[i].Center;

        return centers;
    }
}
