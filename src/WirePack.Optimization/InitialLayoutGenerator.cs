using WirePack.Core.Geometry;

namespace WirePack.Optimization;

/// <summary>
/// Генератор допустимых начальных укладок проводов.
/// </summary>
/// <remarks>
/// Первые три провода размещаются от центра в порядке убывания диаметра. Затем на каждом шаге
/// перебираются все оставшиеся диаметры, для каждого выбирается лучшая позиция, а итоговый выбор
/// делается по коэффициенту заполнения. Позиции с двумя и более касаниями имеют приоритет в
/// пределах 0.1% по коэффициенту заполнения. Последовательность определяется seed, поэтому при
/// одинаковом seed и одинаковых исходных данных генерируется одинаковая последовательность укладок.
/// Повторные вызовы <see cref="Generate"/> возвращают разные укладки.
/// </remarks>
public sealed class InitialLayoutGenerator
{
    private const int FallbackAttempts = 128;
    private const int SeedWireCount = 3;
    private const double OrderJitterFactor = 0.05d;
    private const double FallbackMargin = 1e-6d;
    private const double ContactFillTolerance = 0.001d;
    private const double FillEpsilon = 1e-12d;
    private const double FillRandomJitter = 1e-9d;

    private readonly Random _random;

    private readonly record struct PlacementOption(
        int WireIndex,
        Point Position,
        int Contacts,
        double FillFactor,
        double MecRadius,
        double CenterDistance);

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
        var remaining = new bool[count];
        Array.Fill(remaining, true);
        MecResult? currentMec = null;
        double placedRadiusSquaredSum = 0d;

        int seedCount = Math.Min(SeedWireCount, count);
        for (int k = 0; k < seedCount; k++)
        {
            int index = order[k];
            Point center = placed.Count == 0
                ? Point.Origin
                : FindBestPlacement(wires[index], index, placed, currentMec!, placedRadiusSquaredSum);

            PlaceWire(index, center);
        }

        while (placed.Count < count)
        {
            PlacementOption next = SelectNextPlacement(
                wires,
                remaining,
                placed,
                currentMec!,
                placedRadiusSquaredSum);

            PlaceWire(next.WireIndex, next.Position);
        }

        var placedWires = new Wire[count];
        for (int i = 0; i < count; i++)
            placedWires[i] = wires[i].MoveTo(centers[i]);

        return new Layout(placedWires);

        void PlaceWire(int index, Point center)
        {
            centers[index] = center;
            placed.Add(wires[index].MoveTo(center));
            placedRadiusSquaredSum += wires[index].Radius * wires[index].Radius;
            remaining[index] = false;
            currentMec = NumericalWeightedMecSolver.Solve(placed);

            if (onWirePlaced is not null)
            {
                var partialLayout = new Layout(placed.ToArray());
                var partialTube = new Tube(currentMec.Center, currentMec.Radius);
                onWirePlaced(partialLayout, partialTube);
            }
        }
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

    private Point FindBestPlacement(
        Wire wire,
        int wireIndex,
        List<Wire> placed,
        MecResult currentMec,
        double placedRadiusSquaredSum)
    {
        var options = new List<PlacementOption>();
        AddCandidateOptions(wire, wireIndex, placed, currentMec, placedRadiusSquaredSum, options);

        PlacementOption? best = SelectBestOption(options);
        if (best is not null)
            return best.Value.Position;

        Point? fallback = SelectBestFallback(wire, placed, currentMec);
        if (fallback is not null)
            return fallback.Value;

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

    private static void AddPairTangentCandidates(
        Wire wire,
        List<Wire> placed,
        MecResult currentMec,
        List<Point> candidates)
    {
        double wireRadius = wire.Radius;
        int n = placed.Count;

        if (n <= 30)
        {
            // Мало проводов — проверяем все пары
            for (int a = 0; a < n; a++)
            {
                for (int b = a + 1; b < n; b++)
                {
                    AddTangentPairIfValid(placed, a, b, wire, wire.Radius, candidates);
                }
            }
            return;
        }

        // Пространственная сетка (bucketing) для O(n) поиска пар
        // Размер ячейки = максимальное расстояние взаимодействия
        double maxWireRadius = 0;
        for (int i = 0; i < placed.Count; i++)
            if (placed[i].Radius > maxWireRadius) maxWireRadius = placed[i].Radius;

        double cellSize = 2 * (maxWireRadius + wire.Radius);
        if (cellSize <= 0) return;

        // Определяем границы
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
        for (int i = 0; i < n; i++)
        {
            var c = placed[i].Center;
            if (c.X < minX) minX = c.X;
            if (c.X > maxX) maxX = c.X;
            if (c.Y < minY) minY = c.Y;
            if (c.Y > maxY) maxY = c.Y;
        }

        int gridWidth = (int)Math.Ceiling((maxX - minX) / cellSize) + 1;
        int gridHeight = (int)Math.Ceiling((maxY - minY) / cellSize) + 1;
        if (gridWidth <= 0) gridWidth = 1;
        if (gridHeight <= 0) gridHeight = 1;

        // Плоский массив ячеек
        var grid = new List<int>[gridWidth * gridHeight];
        for (int i = 0; i < grid.Length; i++)
            grid[i] = new List<int>();

        // Размещаем провода в ячейки
        for (int i = 0; i < n; i++)
        {
            var c = placed[i].Center;
            int gx = (int)Math.Floor((c.X - minX) / cellSize);
            int gy = (int)Math.Floor((c.Y - minY) / cellSize);
            if (gx < 0) gx = 0; if (gx >= gridWidth) gx = gridWidth - 1;
            if (gy < 0) gy = 0; if (gy >= gridHeight) gy = gridHeight - 1;
            grid[gx + gy * gridWidth].Add(i);
        }

        // Проверяем пары внутри ячейки и соседних ячеек (4 направления)
        int[] dx = { 0, 1, 1, 1 };
        int[] dy = { 1, 1, 0, -1 };

        for (int gx = 0; gx < gridWidth; gx++)
        {
            for (int gy = 0; gy < gridHeight; gy++)
            {
                var cellIndices = grid[gx + gy * gridWidth];
                int cellCount = cellIndices.Count;

                // Пары внутри ячейки
                for (int ia = 0; ia < cellCount; ia++)
                {
                    int a = cellIndices[ia];
                    // Внутри ячейки
                    for (int ib = ia + 1; ib < cellCount; ib++)
                    {
                        int b = cellIndices[ib];
                        AddTangentPairIfValid(placed, a, b, wire, wire.Radius, candidates);
                    }

                    // Соседние ячейки (4 направления)
                    for (int d = 0; d < 4; d++)
                    {
                        int ncx = gx + dx[d];
                        int ncy = gy + dy[d];
                        if (ncx >= 0 && ncx < gridWidth && ncy >= 0 && ncy < gridHeight)
                        {
                            var neighborIndices = grid[ncx + ncy * gridWidth];
                            foreach (int b in neighborIndices)
                                AddTangentPairIfValid(placed, a, b, wire, wire.Radius, candidates);
                        }
                    }
                }
            }
        }
    }

    private static void AddTangentPairIfValid(
        List<Wire> placed,
        int a,
        int b,
        Wire wire,
        double wireRadius,
        List<Point> candidates)
    {
        Wire first = placed[a];
        Wire second = placed[b];
        double dx = second.Center.X - first.Center.X;
        double dy = second.Center.Y - first.Center.Y;
        double distanceSq = dx * dx + dy * dy;

        double firstRadius = first.Radius + wire.Radius;
        double secondRadius = second.Radius + wire.Radius;
        double maxDist = first.Radius + second.Radius + 2 * wire.Radius;
        double maxDistSq = maxDist * maxDist;

        if (distanceSq > maxDistSq + GeometryTolerance.Eps)
            return;

        double distance = Math.Sqrt(distanceSq);
        if (distance <= GeometryTolerance.Eps)
            return;

        if (distance > firstRadius + secondRadius + GeometryTolerance.Eps)
            return;
        if (distance < Math.Abs(firstRadius - secondRadius) - GeometryTolerance.Eps)
            return;

        double along = (firstRadius * firstRadius - secondRadius * secondRadius + distance * distance)
            / (2d * distance);
        double heightSquared = firstRadius * firstRadius - along * along;
        if (heightSquared < -GeometryTolerance.Eps)
            return;

        double height = Math.Sqrt(Math.Max(0d, heightSquared));
        double ux = dx / distance;
        double uy = dy / distance;
        double baseX = first.Center.X + along * ux;
        double baseY = first.Center.Y + along * uy;
        double px = -uy;
        double py = ux;

        candidates.Add(new Point(first.Center.X + along * ux + height * px, first.Center.Y + along * uy + height * py));
        candidates.Add(new Point(first.Center.X + along * ux - height * px, first.Center.Y + along * uy - height * py));
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

    private void AddCandidateOptions(
        Wire wire,
        int wireIndex,
        List<Wire> placed,
        MecResult currentMec,
        double placedRadiusSquaredSum,
        List<PlacementOption> options)
    {
        var candidates = new List<Point>();
        AddSingleTangentCandidates(wire, placed, currentMec, candidates);
        AddPairTangentCandidates(wire, placed, currentMec, candidates);
        AddCenterCandidate(wire, placed, currentMec, candidates);

        double radiusSquaredSum = placedRadiusSquaredSum + wire.Radius * wire.Radius;
        foreach (Point candidate in candidates)
        {
            if (!Fits(new Circle(candidate, wire.Radius), placed))
                continue;

            double mecRadius = EvaluateMecRadius(placed, currentMec, candidate, wire.Radius);
            if (mecRadius <= 0d)
                continue;

            int contacts = CountContacts(candidate, wire.Radius, placed);
            double fillFactor = radiusSquaredSum / (mecRadius * mecRadius)
                + _random.NextDouble() * FillRandomJitter;
            double centerDistance = candidate.DistanceSquaredTo(currentMec.Center);

            options.Add(new PlacementOption(
                wireIndex,
                candidate,
                contacts,
                fillFactor,
                mecRadius,
                centerDistance));
        }
    }

    private PlacementOption SelectNextPlacement(
        IReadOnlyList<Wire> wires,
        bool[] remaining,
        List<Wire> placed,
        MecResult currentMec,
        double placedRadiusSquaredSum)
    {
        var options = new List<PlacementOption>();
        var evaluatedRadii = new HashSet<double>();

        for (int i = 0; i < wires.Count; i++)
        {
            if (!remaining[i] || !evaluatedRadii.Add(wires[i].Radius))
                continue;

            AddCandidateOptions(wires[i], i, placed, currentMec, placedRadiusSquaredSum, options);
        }

        PlacementOption? best = SelectBestOption(options);
        if (best is not null)
            return best.Value;

        // Резервный вариант: размещаем самый крупный оставшийся провод снаружи.
        int fallbackIndex = -1;
        double fallbackRadius = -1d;
        for (int i = 0; i < wires.Count; i++)
        {
            if (!remaining[i] || wires[i].Radius <= fallbackRadius)
                continue;

            fallbackRadius = wires[i].Radius;
            fallbackIndex = i;
        }

        if (fallbackIndex < 0)
            throw new InvalidOperationException("Не осталось проводов для размещения.");

        Point fallback = SelectBestFallback(wires[fallbackIndex], placed, currentMec)
            ?? PlaceOutside(wires[fallbackIndex], placed);
        double fallbackMecRadius = EvaluateMecRadius(
            placed,
            currentMec,
            fallback,
            wires[fallbackIndex].Radius);
        double fallbackFill = (placedRadiusSquaredSum + wires[fallbackIndex].Radius * wires[fallbackIndex].Radius)
            / (fallbackMecRadius * fallbackMecRadius);

        return new PlacementOption(
            fallbackIndex,
            fallback,
            CountContacts(fallback, wires[fallbackIndex].Radius, placed),
            fallbackFill,
            fallbackMecRadius,
            fallback.DistanceSquaredTo(currentMec.Center));
    }

    private static PlacementOption? SelectBestOption(List<PlacementOption> options)
    {
        PlacementOption? best = null;
        PlacementOption? bestTwoContacts = null;

        foreach (PlacementOption option in options)
        {
            if (best is null || IsBetter(option, best.Value))
                best = option;

            if (option.Contacts >= 2 &&
                (bestTwoContacts is null || IsBetterTwoContacts(option, bestTwoContacts.Value)))
            {
                bestTwoContacts = option;
            }
        }

        if (best is null)
            return null;

        if (bestTwoContacts is not null &&
            bestTwoContacts.Value.FillFactor >= best.Value.FillFactor * (1d - ContactFillTolerance))
        {
            return bestTwoContacts;
        }

        return best;
    }

    private static bool IsBetter(PlacementOption candidate, PlacementOption current)
    {
        if (candidate.FillFactor > current.FillFactor + FillEpsilon)
            return true;

        if (Math.Abs(candidate.FillFactor - current.FillFactor) > FillEpsilon)
            return false;

        if (candidate.Contacts != current.Contacts)
            return candidate.Contacts > current.Contacts;

        if (candidate.MecRadius < current.MecRadius - FillEpsilon)
            return true;

        if (Math.Abs(candidate.MecRadius - current.MecRadius) > FillEpsilon)
            return false;

        return candidate.CenterDistance < current.CenterDistance;
    }

    private static bool IsBetterTwoContacts(PlacementOption candidate, PlacementOption current)
    {
        if (candidate.FillFactor > current.FillFactor + FillEpsilon)
            return true;

        if (Math.Abs(candidate.FillFactor - current.FillFactor) > FillEpsilon)
            return false;

        if (candidate.MecRadius < current.MecRadius - FillEpsilon)
            return true;

        if (Math.Abs(candidate.MecRadius - current.MecRadius) > FillEpsilon)
            return false;

        return candidate.CenterDistance < current.CenterDistance;
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