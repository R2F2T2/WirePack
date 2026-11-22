using WirePack.Core.Geometry;

namespace WirePack.Optimization;

/// <summary>
/// Генератор допустимых начальных укладок проводов.
/// </summary>
/// <remarks>
/// Провода размещаются последовательно от центра: сначала более крупные, затем более мелкие.
/// Для каждого провода рассматриваются позиции, касающиеся уже размещённых проводов, и выбирается
/// позиция, дающая минимальный радиус MEC. Последовательность определяется seed, поэтому при
/// одинаковом seed и одинаковых исходных данных генерируется одинаковая последовательность укладок.
/// Повторные вызовы <see cref="Generate"/> возвращают разные укладки.
/// </remarks>
public sealed class InitialLayoutGenerator
{
    private const int MaxPairNeighborCount = 8;
    private const int MaxRandomPairCount = 4;
    private const int FallbackAttempts = 128;
    private const double OrderJitterFactor = 0.05d;
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
        var centers = new Point[count];
        var placed = new List<Wire>(count);
        int[] order = BuildPlacementOrder(wires);
        MecResult? currentMec = null;

        for (int k = 0; k < count; k++)
        {
            int index = order[k];
            Wire wire = wires[index];
            Point center = placed.Count == 0
                ? Point.Origin
                : FindBestPlacement(wire, placed, currentMec!);

            centers[index] = center;
            placed.Add(wire.MoveTo(center));
            currentMec = NumericalWeightedMecSolver.Solve(placed);

            if (onWirePlaced is not null)
            {
                var partialLayout = new Layout(placed.ToArray());
                var partialTube = new Tube(currentMec.Center, currentMec.Radius);
                onWirePlaced(partialLayout, partialTube);
            }
        }

        var placedWires = new Wire[count];
        for (int i = 0; i < count; i++)
            placedWires[i] = wires[i].MoveTo(centers[i]);

        return new Layout(placedWires);
    }

    private int[] BuildPlacementOrder(IReadOnlyList<Wire> wires)
    {
        int count = wires.Count;
        double maxRadius = 0d;
        for (int i = 0; i < count; i++)
            maxRadius = Math.Max(maxRadius, wires[i].Radius);

        double jitter = maxRadius * OrderJitterFactor;
        var keys = new double[count];
        for (int i = 0; i < count; i++)
            keys[i] = wires[i].Radius + _random.NextDouble() * jitter;

        var order = new int[count];
        for (int i = 0; i < count; i++)
            order[i] = i;

        Array.Sort(order, (first, second) =>
        {
            int comparison = keys[second].CompareTo(keys[first]);
            return comparison != 0 ? comparison : first.CompareTo(second);
        });

        return order;
    }

    private Point FindBestPlacement(Wire wire, List<Wire> placed, MecResult currentMec)
    {
        var candidates = new List<Point>();
        AddSingleTangentCandidates(wire, placed, currentMec, candidates);
        AddPairTangentCandidates(wire, placed, currentMec, candidates);
        AddCenterCandidate(wire, placed, currentMec, candidates);

        Point? best = SelectBestCandidate(wire, placed, currentMec, candidates);
        if (best is not null)
            return best.Value;

        best = SelectBestFallback(wire, placed, currentMec);
        if (best is not null)
            return best.Value;

        return PlaceOutside(wire, placed);
    }

    private static void AddSingleTangentCandidates(
        Wire wire,
        List<Wire> placed,
        MecResult currentMec,
        List<Point> candidates)
    {
        for (int i = 0; i < placed.Count; i++)
        {
            Wire other = placed[i];
            double dx = currentMec.Center.X - other.Center.X;
            double dy = currentMec.Center.Y - other.Center.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);

            double ux;
            double uy;
            if (length <= GeometryTolerance.Eps)
            {
                double angle = i * 2.399963229728653d;
                ux = Math.Cos(angle);
                uy = Math.Sin(angle);
            }
            else
            {
                ux = dx / length;
                uy = dy / length;
            }

            double distance = other.Radius + wire.Radius;
            candidates.Add(new Point(other.Center.X + ux * distance, other.Center.Y + uy * distance));
            candidates.Add(new Point(other.Center.X - ux * distance, other.Center.Y - uy * distance));
        }
    }

    private void AddPairTangentCandidates(
        Wire wire,
        List<Wire> placed,
        MecResult currentMec,
        List<Point> candidates)
    {
        List<int> indices = SelectPairCandidates(placed, currentMec);

        for (int a = 0; a < indices.Count; a++)
        {
            Wire first = placed[indices[a]];
            for (int b = a + 1; b < indices.Count; b++)
            {
                Wire second = placed[indices[b]];
                double dx = second.Center.X - first.Center.X;
                double dy = second.Center.Y - first.Center.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance <= GeometryTolerance.Eps)
                    continue;

                double firstRadius = first.Radius + wire.Radius;
                double secondRadius = second.Radius + wire.Radius;
                if (distance > firstRadius + secondRadius + GeometryTolerance.Eps)
                    continue;
                if (distance < Math.Abs(firstRadius - secondRadius) - GeometryTolerance.Eps)
                    continue;

                double along = (firstRadius * firstRadius - secondRadius * secondRadius + distance * distance)
                    / (2d * distance);
                double heightSquared = firstRadius * firstRadius - along * along;
                if (heightSquared < -GeometryTolerance.Eps)
                    continue;

                double height = Math.Sqrt(Math.Max(0d, heightSquared));
                double ux = dx / distance;
                double uy = dy / distance;
                double baseX = first.Center.X + along * ux;
                double baseY = first.Center.Y + along * uy;
                double px = -uy;
                double py = ux;

                candidates.Add(new Point(baseX + height * px, baseY + height * py));
                candidates.Add(new Point(baseX - height * px, baseY - height * py));
            }
        }
    }

    private List<int> SelectPairCandidates(List<Wire> placed, MecResult currentMec)
    {
        var selected = new HashSet<int>();
        foreach (int index in currentMec.SupportingWireIndices)
        {
            if (index >= 0 && index < placed.Count)
                selected.Add(index);
        }

        int closestCount = Math.Min(MaxPairNeighborCount, placed.Count);
        for (int k = 0; k < closestCount; k++)
        {
            int bestIndex = -1;
            double bestDistance = double.PositiveInfinity;
            for (int i = 0; i < placed.Count; i++)
            {
                if (selected.Contains(i))
                    continue;

                double distance = placed[i].Center.DistanceSquaredTo(currentMec.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
                break;

            selected.Add(bestIndex);
        }

        int randomCount = Math.Min(MaxRandomPairCount, placed.Count);
        for (int k = 0; k < randomCount; k++)
            selected.Add(_random.Next(placed.Count));

        var result = new List<int>(selected.Count);
        foreach (int index in selected)
            result.Add(index);

        return result;
    }

    private static void AddCenterCandidate(
        Wire wire,
        List<Wire> placed,
        MecResult currentMec,
        List<Point> candidates)
    {
        if (Fits(new Circle(currentMec.Center, wire.Radius), placed))
            candidates.Add(currentMec.Center);
    }

    private Point? SelectBestCandidate(
        Wire wire,
        List<Wire> placed,
        MecResult currentMec,
        List<Point> candidates)
    {
        Point? best = null;
        double bestScore = double.PositiveInfinity;
        int bestContacts = -1;
        double bestCenterDistance = double.PositiveInfinity;

        // Радиус MEC остаётся главным критерием. В пределах 0.5% от лучшего радиуса
        // предпочтение отдаётся кандидату с большим числом касаний: 3 касания лучше 2,
        // 4 лучше 3 и т.д. За пределами этого допуска радиус важнее касаний.
        double tieTolerance = Math.Max(1e-9d, currentMec.Radius * 1e-6d);
        double contactBand = Math.Max(tieTolerance, currentMec.Radius * 0.005d);

        foreach (Point candidate in candidates)
        {
            if (!Fits(new Circle(candidate, wire.Radius), placed))
                continue;

            double score = EvaluateMecRadius(placed, currentMec, candidate, wire.Radius)
                + _random.NextDouble() * tieTolerance;
            int contacts = CountContacts(candidate, wire.Radius, placed);
            double centerDistance = candidate.DistanceSquaredTo(currentMec.Center);

            bool radiusClearlyBetter = score < bestScore - contactBand;
            bool radiusComparable = Math.Abs(score - bestScore) <= contactBand;

            if (radiusClearlyBetter ||
                (radiusComparable && contacts > bestContacts) ||
                (radiusComparable && contacts == bestContacts &&
                 (score < bestScore - tieTolerance ||
                  (Math.Abs(score - bestScore) <= tieTolerance &&
                   centerDistance < bestCenterDistance - tieTolerance))))
            {
                best = candidate;
                bestScore = score;
                bestContacts = contacts;
                bestCenterDistance = centerDistance;
            }
        }

        return best;
    }

    private static double EvaluateMecRadius(
        List<Wire> placed,
        MecResult currentMec,
        Point candidate,
        double wireRadius)
    {
        double distance = candidate.DistanceTo(currentMec.Center);
        if (GeometryTolerance.IsLessOrEqual(distance + wireRadius, currentMec.Radius))
            return currentMec.Radius;

        var support = new List<Wire>(currentMec.SupportingWireIndices.Count + 1);
        foreach (int index in currentMec.SupportingWireIndices)
        {
            if (index >= 0 && index < placed.Count)
                support.Add(placed[index]);
        }

        support.Add(new Wire(candidate, wireRadius * 2d));

        return support.Count == 0
            ? currentMec.Radius
            : ExactWeightedMecSolver.Solve(support).Radius;
    }

    private static int CountContacts(Point candidate, double wireRadius, List<Wire> placed)
    {
        int contacts = 0;
        foreach (Wire other in placed)
        {
            double distance = candidate.DistanceTo(other.Center);
            double required = wireRadius + other.Radius;
            double tolerance = 1e-6d * Math.Max(1d, required);
            if (Math.Abs(distance - required) <= tolerance)
                contacts++;
        }

        return contacts;
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

    private Point? SelectBestFallback(Wire wire, List<Wire> placed, MecResult currentMec)
    {
        Point? best = null;
        double bestScore = double.PositiveInfinity;
        double baseRadius = currentMec.Radius + wire.Radius + FallbackMargin;

        for (int attempt = 0; attempt < FallbackAttempts; attempt++)
        {
            double angle = _random.NextDouble() * 2d * Math.PI;
            double radius = baseRadius * (1d + 0.25d * _random.NextDouble());
            var candidate = new Point(
                currentMec.Center.X + radius * Math.Cos(angle),
                currentMec.Center.Y + radius * Math.Sin(angle));

            if (!Fits(new Circle(candidate, wire.Radius), placed))
                continue;

            double score = EvaluateMecRadius(placed, currentMec, candidate, wire.Radius);
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private Point PlaceOutside(Wire wire, List<Wire> placed)
    {
        double extent = 0d;
        foreach (Wire other in placed)
        {
            double distance = other.Center.DistanceTo(Point.Origin);
            extent = Math.Max(extent, distance + other.Radius);
        }

        double ringDistance = extent + wire.Radius + FallbackMargin;
        double angle = _random.NextDouble() * 2d * Math.PI;

        return new Point(
            ringDistance * Math.Cos(angle),
            ringDistance * Math.Sin(angle));
    }
}
