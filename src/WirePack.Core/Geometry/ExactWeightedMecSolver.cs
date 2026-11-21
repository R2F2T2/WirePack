namespace WirePack.Core.Geometry;

/// <summary>
/// Эталонный алгоритм вычисления взвешенной MEC перебором опорных наборов.
/// </summary>
/// <remarks>
/// Используется для проверки численного алгоритма и в тестах. Для больших наборов
/// сложность перебора кубическая, поэтому в оптимизаторе применяется численный алгоритм.
/// </remarks>
public static class ExactWeightedMecSolver
{
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

        int count = wires.Count;
        var pairRadii = new double[count, count];
        MecResult? best = null;

        for (int i = 0; i < count; i++)
            Consider(new MecResult(wires[i].Center, wires[i].Radius, new[] { i }), wires, ref best);

        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                if (!WeightedMecSupport.TryCreatePair(wires[i], i, wires[j], j, out MecResult? pair) || pair == null)
                    continue;

                pairRadii[i, j] = pair.Radius;
                pairRadii[j, i] = pair.Radius;
                Consider(pair, wires, ref best);
            }
        }

        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                for (int k = j + 1; k < count; k++)
                {
                    double lowerBound = Math.Max(
                        pairRadii[i, j],
                        Math.Max(pairRadii[i, k], pairRadii[j, k]));

                    if (best != null && lowerBound >= best.Radius - GeometryTolerance.Eps)
                        continue;

                    var centroid = new Point(
                        (wires[i].Center.X + wires[j].Center.X + wires[k].Center.X) / 3d,
                        (wires[i].Center.Y + wires[j].Center.Y + wires[k].Center.Y) / 3d);

                    if (WeightedMecSupport.TryCreateTriple(
                            wires[i],
                            i,
                            wires[j],
                            j,
                            wires[k],
                            k,
                            centroid,
                            out MecResult? triple) &&
                        triple != null)
                    {
                        Consider(triple, wires, ref best);
                    }

                    if (TryGetCircumcenter(wires[i].Center, wires[j].Center, wires[k].Center, out Point circumcenter) &&
                        WeightedMecSupport.TryCreateTriple(
                            wires[i],
                            i,
                            wires[j],
                            j,
                            wires[k],
                            k,
                            circumcenter,
                            out triple) &&
                        triple != null)
                    {
                        Consider(triple, wires, ref best);
                    }
                }
            }
        }

        return best ?? throw new InvalidOperationException("Не удалось вычислить MEC для заданного набора проводов.");
    }

    private static void Consider(MecResult candidate, IReadOnlyList<Wire> wires, ref MecResult? best)
    {
        if (!WeightedMecSupport.EnclosesAll(candidate, wires))
            return;

        if (best == null || candidate.Radius < best.Radius - GeometryTolerance.Eps)
            best = candidate;
    }

    private static bool TryGetCircumcenter(Point a, Point b, Point c, out Point center)
    {
        center = default;

        double ax = a.X;
        double ay = a.Y;
        double bx = b.X;
        double by = b.Y;
        double cx = c.X;
        double cy = c.Y;

        double determinant = 2d * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
        double scale = Math.Max(1d, Math.Max(
            Math.Max(Math.Abs(ax), Math.Abs(ay)),
            Math.Max(
                Math.Max(Math.Abs(bx), Math.Abs(by)),
                Math.Max(Math.Abs(cx), Math.Abs(cy)))));

        if (Math.Abs(determinant) <= 1e-12 * scale * scale)
            return false;

        double a2 = ax * ax + ay * ay;
        double b2 = bx * bx + by * by;
        double c2 = cx * cx + cy * cy;

        double ux = (a2 * (by - cy) + b2 * (cy - ay) + c2 * (ay - by)) / determinant;
        double uy = (a2 * (cx - bx) + b2 * (ax - cx) + c2 * (bx - ax)) / determinant;

        center = new Point(ux, uy);
        return center.IsFinite;
    }
}
