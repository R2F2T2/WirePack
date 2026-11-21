namespace WirePack.Core.Geometry;

/// <summary>
/// Численный алгоритм вычисления взвешенной минимальной охватывающей окружности.
/// </summary>
/// <remarks>
/// Минимизируется выпуклая функция f(c) = max_i(|p_i - c| + r_i) методом центроидных отсечений.
/// Полученный центр уточняется перебором опорных наборов из одного, двух и трёх проводов.
/// </remarks>
public static class NumericalWeightedMecSolver
{
    private const int MaxCuttingPlaneIterations = 500;
    private const int ExtraCuttingPlaneIterations = 200;
    private const int RefinementCandidateCount = 10;

    /// <summary>
    /// Вычисляет взвешенную MEC для компоновки.
    /// </summary>
    public static MecResult Solve(Layout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        return Solve(layout.Wires);
    }

    /// <summary>
    /// Вычисляет взвешенную MEC для набора проводов.
    /// </summary>
    public static MecResult Solve(IReadOnlyList<Wire> wires)
    {
        ArgumentNullException.ThrowIfNull(wires);
        if (wires.Count == 0)
            throw new ArgumentException("Нельзя вычислить MEC для пустого набора проводов.", nameof(wires));

        if (wires.Count == 1)
            return new MecResult(wires[0].Center, wires[0].Radius, new[] { 0 });

        double upperBound = WeightedMecSupport.RadiusAt(wires[0].Center, wires);
        if (upperBound <= GeometryTolerance.Eps)
            return WeightedMecSupport.CreateFromCenter(wires, wires[0].Center);

        var polygon = CreateInitialBox(wires[0].Center, upperBound);
        double targetSize = Math.Max(1e-10, upperBound * 1e-12);

        polygon = RunCuttingPlane(wires, polygon, targetSize, MaxCuttingPlaneIterations);
        Point center = PolygonCentroid(polygon);
        int[] candidateIndices = WeightedMecSupport.FindFarthestIndices(wires, center, RefinementCandidateCount);

        if (WeightedMecSupport.TryRefine(wires, candidateIndices, center, out MecResult? refined) && refined != null)
            return refined;

        double extraTargetSize = Math.Max(1e-12, targetSize * 1e-3);
        polygon = RunCuttingPlane(wires, polygon, extraTargetSize, ExtraCuttingPlaneIterations);
        center = PolygonCentroid(polygon);
        candidateIndices = WeightedMecSupport.FindFarthestIndices(
            wires,
            center,
            Math.Min(wires.Count, RefinementCandidateCount + 4));

        if (WeightedMecSupport.TryRefine(wires, candidateIndices, center, out refined) && refined != null)
            return refined;

        return WeightedMecSupport.CreateFromCenter(wires, center);
    }

    private static List<Point> RunCuttingPlane(
        IReadOnlyList<Wire> wires,
        List<Point> polygon,
        double targetSize,
        int maxIterations)
    {
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            if (polygon.Count < 3)
                break;

            Point center = PolygonCentroid(polygon);
            int farthest = FindFarthestWire(wires, center, out double centerDistance);

            double gx;
            double gy;
            if (centerDistance <= GeometryTolerance.Eps)
            {
                gx = 1d;
                gy = 0d;
            }
            else
            {
                gx = (center.X - wires[farthest].Center.X) / centerDistance;
                gy = (center.Y - wires[farthest].Center.Y) / centerDistance;
            }

            polygon = ClipPolygon(polygon, center, gx, gy);
            if (polygon.Count < 3)
                break;

            if (BoundingBoxDiagonal(polygon) <= targetSize)
                break;
        }

        return polygon;
    }

    private static int FindFarthestWire(IReadOnlyList<Wire> wires, Point center, out double centerDistance)
    {
        int farthest = 0;
        double maxValue = double.NegativeInfinity;
        centerDistance = 0d;

        for (int i = 0; i < wires.Count; i++)
        {
            double distance = center.DistanceTo(wires[i].Center);
            double value = distance + wires[i].Radius;
            if (value > maxValue)
            {
                maxValue = value;
                farthest = i;
                centerDistance = distance;
            }
        }

        return farthest;
    }

    private static List<Point> CreateInitialBox(Point center, double radius)
    {
        double halfSide = Math.Max(radius, 1e-12);
        return new List<Point>
        {
            new(center.X - halfSide, center.Y - halfSide),
            new(center.X + halfSide, center.Y - halfSide),
            new(center.X + halfSide, center.Y + halfSide),
            new(center.X - halfSide, center.Y + halfSide)
        };
    }

    private static List<Point> ClipPolygon(
        IReadOnlyList<Point> polygon,
        Point center,
        double gx,
        double gy)
    {
        var result = new List<Point>(polygon.Count + 1);

        for (int i = 0; i < polygon.Count; i++)
        {
            Point current = polygon[i];
            Point next = polygon[(i + 1) % polygon.Count];

            double currentValue = gx * (current.X - center.X) + gy * (current.Y - center.Y);
            double nextValue = gx * (next.X - center.X) + gy * (next.Y - center.Y);

            bool currentInside = currentValue <= 0d;
            bool nextInside = nextValue <= 0d;

            if (currentInside)
                result.Add(current);

            if (currentInside != nextInside)
            {
                double denominator = currentValue - nextValue;
                if (Math.Abs(denominator) > double.Epsilon)
                {
                    double t = currentValue / denominator;
                    result.Add(new Point(
                        current.X + t * (next.X - current.X),
                        current.Y + t * (next.Y - current.Y)));
                }
            }
        }

        return RemoveDuplicateVertices(result);
    }

    private static List<Point> RemoveDuplicateVertices(List<Point> points)
    {
        if (points.Count <= 1)
            return points;

        var cleaned = new List<Point>(points.Count);
        foreach (Point point in points)
        {
            if (cleaned.Count == 0 || cleaned[^1].DistanceTo(point) > GeometryTolerance.Eps)
                cleaned.Add(point);
        }

        if (cleaned.Count > 1 && cleaned[0].DistanceTo(cleaned[^1]) <= GeometryTolerance.Eps)
            cleaned.RemoveAt(cleaned.Count - 1);

        return cleaned;
    }

    private static Point PolygonCentroid(IReadOnlyList<Point> polygon)
    {
        if (polygon.Count == 0)
            throw new InvalidOperationException("Многоугольник отсечений пуст.");

        if (polygon.Count == 1)
            return polygon[0];

        if (polygon.Count == 2)
        {
            return new Point(
                (polygon[0].X + polygon[1].X) / 2d,
                (polygon[0].Y + polygon[1].Y) / 2d);
        }

        double areaTwice = 0d;
        double cx = 0d;
        double cy = 0d;

        for (int i = 0; i < polygon.Count; i++)
        {
            Point current = polygon[i];
            Point next = polygon[(i + 1) % polygon.Count];
            double cross = current.X * next.Y - next.X * current.Y;
            areaTwice += cross;
            cx += (current.X + next.X) * cross;
            cy += (current.Y + next.Y) * cross;
        }

        if (Math.Abs(areaTwice) <= 1e-18)
        {
            double sumX = 0d;
            double sumY = 0d;
            foreach (Point point in polygon)
            {
                sumX += point.X;
                sumY += point.Y;
            }

            return new Point(sumX / polygon.Count, sumY / polygon.Count);
        }

        return new Point(cx / (3d * areaTwice), cy / (3d * areaTwice));
    }

    private static double BoundingBoxDiagonal(IReadOnlyList<Point> polygon)
    {
        if (polygon.Count == 0)
            return 0d;

        double minX = polygon[0].X;
        double maxX = minX;
        double minY = polygon[0].Y;
        double maxY = minY;

        for (int i = 1; i < polygon.Count; i++)
        {
            Point point = polygon[i];
            if (point.X < minX) minX = point.X;
            if (point.X > maxX) maxX = point.X;
            if (point.Y < minY) minY = point.Y;
            if (point.Y > maxY) maxY = point.Y;
        }

        double dx = maxX - minX;
        double dy = maxY - minY;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
