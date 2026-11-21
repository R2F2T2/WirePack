namespace WirePack.Core.Geometry;

/// <summary>
/// Вспомогательные операции для вычисления взвешенной MEC по опорным проводам.
/// </summary>
internal static class WeightedMecSupport
{
    private const int TripleNewtonIterations = 80;
    private const double NewtonConvergence = 1e-13;

    /// <summary>
    /// Строит минимальную охватывающую окружность для двух проводов.
    /// Если один провод содержит другой, возвращается окружность большего провода.
    /// </summary>
    public static bool TryCreatePair(
        Wire first,
        int firstIndex,
        Wire second,
        int secondIndex,
        out MecResult? candidate)
    {
        candidate = null;

        double distance = first.Center.DistanceTo(second.Center);
        if (distance <= GeometryTolerance.Eps)
        {
            candidate = first.Radius >= second.Radius
                ? new MecResult(first.Center, first.Radius, new[] { firstIndex })
                : new MecResult(second.Center, second.Radius, new[] { secondIndex });
            return true;
        }

        double radiusDifference = Math.Abs(first.Radius - second.Radius);
        if (radiusDifference >= distance)
        {
            candidate = first.Radius > second.Radius
                ? new MecResult(first.Center, first.Radius, new[] { firstIndex })
                : new MecResult(second.Center, second.Radius, new[] { secondIndex });
            return true;
        }

        double radius = (distance + first.Radius + second.Radius) / 2d;
        double offsetFromFirst = (distance + second.Radius - first.Radius) / 2d;
        double ux = (second.Center.X - first.Center.X) / distance;
        double uy = (second.Center.Y - first.Center.Y) / distance;
        var center = new Point(
            first.Center.X + offsetFromFirst * ux,
            first.Center.Y + offsetFromFirst * uy);

        candidate = new MecResult(center, radius, new[] { firstIndex, secondIndex });
        return true;
    }

    /// <summary>
    /// Строит окружность, внешне касающуюся трёх проводов.
    /// </summary>
    public static bool TryCreateTriple(
        Wire first,
        int firstIndex,
        Wire second,
        int secondIndex,
        Wire third,
        int thirdIndex,
        Point initialCenter,
        out MecResult? candidate)
    {
        candidate = null;

        if (!initialCenter.IsFinite)
            return false;

        Point p0 = first.Center;
        Point p1 = second.Center;
        Point p2 = third.Center;
        double r0 = first.Radius;
        double r1 = second.Radius;
        double r2 = third.Radius;

        Point center = initialCenter;
        bool converged = false;

        for (int iteration = 0; iteration < TripleNewtonIterations; iteration++)
        {
            double d0 = center.DistanceTo(p0);
            double d1 = center.DistanceTo(p1);
            double d2 = center.DistanceTo(p2);

            if (d0 <= GeometryTolerance.Eps || d1 <= GeometryTolerance.Eps || d2 <= GeometryTolerance.Eps)
                return false;

            double f1 = d0 - d1 - (r1 - r0);
            double f2 = d0 - d2 - (r2 - r0);

            double u0x = (center.X - p0.X) / d0;
            double u0y = (center.Y - p0.Y) / d0;
            double u1x = (center.X - p1.X) / d1;
            double u1y = (center.Y - p1.Y) / d1;
            double u2x = (center.X - p2.X) / d2;
            double u2y = (center.Y - p2.Y) / d2;

            double j11 = u0x - u1x;
            double j12 = u0y - u1y;
            double j21 = u0x - u2x;
            double j22 = u0y - u2y;

            double determinant = j11 * j22 - j12 * j21;
            if (Math.Abs(determinant) <= 1e-14)
                return false;

            double ex = -f1;
            double ey = -f2;
            double dx = (ex * j22 - j12 * ey) / determinant;
            double dy = (j11 * ey - ex * j21) / determinant;

            center = center.Translate(dx, dy);
            if (!center.IsFinite)
                return false;

            double scale = Math.Max(1d, d0 + d1 + d2);
            if (Math.Sqrt(dx * dx + dy * dy) <= NewtonConvergence * scale)
            {
                converged = true;
                break;
            }
        }

        if (!converged)
            return false;

        double value0 = center.DistanceTo(p0) + r0;
        double value1 = center.DistanceTo(p1) + r1;
        double value2 = center.DistanceTo(p2) + r2;
        double radius = (value0 + value1 + value2) / 3d;
        double tolerance = 1e-8 + 1e-10 * Math.Abs(radius);

        if (Math.Abs(value0 - radius) > tolerance ||
            Math.Abs(value1 - radius) > tolerance ||
            Math.Abs(value2 - radius) > tolerance)
        {
            return false;
        }

        candidate = new MecResult(center, radius, new[] { firstIndex, secondIndex, thirdIndex });
        return true;
    }

    /// <summary>
    /// Проверяет, что окружность охватывает все провода с учётом допуска.
    /// </summary>
    public static bool EnclosesAll(MecResult candidate, IReadOnlyList<Wire> wires)
    {
        for (int i = 0; i < wires.Count; i++)
        {
            double distance = candidate.Center.DistanceTo(wires[i].Center);
            if (!GeometryTolerance.IsLessOrEqual(distance + wires[i].Radius, candidate.Radius))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Возвращает максимальное значение |p_i - c| + r_i.
    /// </summary>
    public static double RadiusAt(Point center, IReadOnlyList<Wire> wires)
    {
        double radius = 0d;
        for (int i = 0; i < wires.Count; i++)
        {
            double value = center.DistanceTo(wires[i].Center) + wires[i].Radius;
            if (value > radius)
                radius = value;
        }

        return radius;
    }

    /// <summary>
    /// Возвращает индексы наиболее удалённых по значению |p_i - c| + r_i проводов.
    /// </summary>
    public static int[] FindFarthestIndices(IReadOnlyList<Wire> wires, Point center, int count)
    {
        int effectiveCount = Math.Min(count, wires.Count);
        var distances = new double[wires.Count];
        var indices = new int[wires.Count];

        for (int i = 0; i < wires.Count; i++)
        {
            distances[i] = center.DistanceTo(wires[i].Center) + wires[i].Radius;
            indices[i] = i;
        }

        Array.Sort(indices, (left, right) =>
        {
            int comparison = distances[right].CompareTo(distances[left]);
            return comparison != 0 ? comparison : left.CompareTo(right);
        });

        var result = new int[effectiveCount];
        Array.Copy(indices, result, effectiveCount);
        return result;
    }

    /// <summary>
    /// Строит окружность с заданным центром и минимальным радиусом, охватывающим все провода.
    /// </summary>
    public static MecResult CreateFromCenter(IReadOnlyList<Wire> wires, Point center)
    {
        double radius = RadiusAt(center, wires);
        var supportingIndices = new List<int>();

        for (int i = 0; i < wires.Count; i++)
        {
            double value = center.DistanceTo(wires[i].Center) + wires[i].Radius;
            if (GeometryTolerance.AreClose(value, radius))
                supportingIndices.Add(i);
        }

        return new MecResult(center, radius, supportingIndices);
    }

    /// <summary>
    /// Уточняет MEC перебором опорных наборов из заданных кандидатов.
    /// </summary>
    public static bool TryRefine(
        IReadOnlyList<Wire> wires,
        IReadOnlyList<int> candidateIndices,
        Point initialCenter,
        out MecResult? best)
    {
        best = null;
        int count = candidateIndices.Count;

        for (int a = 0; a < count; a++)
        {
            int ia = candidateIndices[a];
            Consider(new MecResult(wires[ia].Center, wires[ia].Radius, new[] { ia }), wires, ref best);
        }

        for (int a = 0; a < count; a++)
        {
            int ia = candidateIndices[a];
            for (int b = a + 1; b < count; b++)
            {
                int ib = candidateIndices[b];
                if (TryCreatePair(wires[ia], ia, wires[ib], ib, out MecResult? pair) && pair != null)
                    Consider(pair, wires, ref best);
            }
        }

        for (int a = 0; a < count; a++)
        {
            int ia = candidateIndices[a];
            for (int b = a + 1; b < count; b++)
            {
                int ib = candidateIndices[b];
                for (int c = b + 1; c < count; c++)
                {
                    int ic = candidateIndices[c];
                    if (TryCreateTriple(
                            wires[ia],
                            ia,
                            wires[ib],
                            ib,
                            wires[ic],
                            ic,
                            initialCenter,
                            out MecResult? triple) &&
                        triple != null)
                    {
                        Consider(triple, wires, ref best);
                    }
                }
            }
        }

        return best != null;
    }

    private static void Consider(MecResult candidate, IReadOnlyList<Wire> wires, ref MecResult? best)
    {
        if (!EnclosesAll(candidate, wires))
            return;

        if (best == null || candidate.Radius < best.Radius - GeometryTolerance.Eps)
            best = candidate;
    }
}
