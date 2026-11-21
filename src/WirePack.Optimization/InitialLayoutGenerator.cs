using WirePack.Core.Geometry;

namespace WirePack.Optimization;

/// <summary>
/// Генератор допустимых начальных укладок проводов.
/// </summary>
/// <remarks>
/// Провода размещаются последовательно в случайном порядке с проверкой отсутствия пересечений
/// средствами <see cref="GeometryOperations"/>. Последовательность случайных чисел определяется
/// seed, поэтому при одинаковом seed и одинаковых исходных данных генерируется одинаковая
/// последовательность укладок. Повторные вызовы <see cref="Generate"/> возвращают разные укладки.
/// </remarks>
public sealed class InitialLayoutGenerator
{
    private const int MaxAttemptsPerWire = 1000;
    private const int AttemptsPerGrowth = 100;
    private const double SearchRadiusGrowth = 1.5d;
    private const double FallbackMargin = 1e-6d;

    private readonly Random _random;

    /// <summary>
    /// Создаёт генератор с фиксированным seed.
    /// </summary>
    public InitialLayoutGenerator(int seed)
    {
        _random = new Random(seed);
    }

    /// <summary>
    /// Строит очередную допустимую укладку для заданного набора проводов.
    /// </summary>
    /// <param name="wires">Провода с диаметрами, для которых нужно построить укладку.</param>
    /// <param name="onWirePlaced">Опциональный callback для отслеживания добавления каждого провода.</param>
    /// <returns>
    /// Укладка без пересечений. Порядок проводов и их диаметры соответствуют исходному набору.
    /// </returns>
    public Layout Generate(IReadOnlyList<Wire> wires, Action<Layout, Tube?>? onWirePlaced = null)
    {
        ArgumentNullException.ThrowIfNull(wires);
        if (wires.Count == 0)
            throw new ArgumentException("Набор проводов не должен быть пустым.", nameof(wires));

        int count = wires.Count;
        var order = new int[count];
        for (int i = 0; i < count; i++)
            order[i] = i;

        Shuffle(order);

        var centers = new Point[count];
        var placed = new List<Wire>(count);
        double searchRadius = ComputeInitialSearchRadius(wires);

        for (int k = 0; k < count; k++)
        {
            int index = order[k];
            Wire wire = wires[index];
            Point center = PlaceWire(wire, placed, searchRadius);
            centers[index] = center;
            placed.Add(wire.MoveTo(center));

            if (onWirePlaced is not null)
            {
                var partialLayout = new Layout(placed.ToArray());
                Tube? partialTube = null;
                if (partialLayout.Count > 0)
                {
                    var mec = NumericalWeightedMecSolver.Solve(partialLayout);
                    partialTube = new Tube(mec.Center, mec.Radius);
                }
                onWirePlaced(partialLayout, partialTube);
            }
        }

        var placedWires = new Wire[count];
        for (int i = 0; i < count; i++)
            placedWires[i] = wires[i].MoveTo(centers[i]);

        return new Layout(placedWires);
    }

    private Point PlaceWire(Wire wire, List<Wire> placed, double searchRadius)
    {
        var candidate = new Circle(Point.Origin, wire.Radius);
        double radius = searchRadius;

        for (int attempt = 1; attempt <= MaxAttemptsPerWire; attempt++)
        {
            Point center = SampleDisk(radius);
            candidate = new Circle(center, wire.Radius);

            if (Fits(candidate, placed))
                return center;

            if (attempt % AttemptsPerGrowth == 0)
                radius *= SearchRadiusGrowth;
        }

        return PlaceOutside(wire, placed);
    }

    private static bool Fits(Circle candidate, List<Wire> placed)
    {
        foreach (Wire other in placed)
        {
            if (GeometryOperations.CirclesIntersect(candidate, other.Circle))
                return false;
        }

        return true;
    }

    private Point PlaceOutside(Wire wire, List<Wire> placed)
    {
        double extent = 0d;
        foreach (Wire other in placed)
        {
            double distance = other.Center.DistanceTo(Point.Origin);
            extent = Math.Max(extent, distance + other.Radius);
        }

        // Провод на внешнем кольце гарантированно не пересекает уже размещённые провода.
        double ringDistance = extent + wire.Radius + FallbackMargin;
        double angle = _random.NextDouble() * 2d * Math.PI;

        return new Point(
            ringDistance * Math.Cos(angle),
            ringDistance * Math.Sin(angle));
    }

    private Point SampleDisk(double radius)
    {
        double distance = radius * Math.Sqrt(_random.NextDouble());
        double angle = _random.NextDouble() * 2d * Math.PI;

        return new Point(
            distance * Math.Cos(angle),
            distance * Math.Sin(angle));
    }

    private static double ComputeInitialSearchRadius(IReadOnlyList<Wire> wires)
    {
        double sumSquares = 0d;
        double maxRadius = 0d;

        foreach (Wire wire in wires)
        {
            sumSquares += wire.Radius * wire.Radius;
            maxRadius = Math.Max(maxRadius, wire.Radius);
        }

        double areaRadius = 2d * Math.Sqrt(sumSquares);
        return Math.Max(areaRadius, 2d * maxRadius);
    }

    private void Shuffle(int[] values)
    {
        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }
}
