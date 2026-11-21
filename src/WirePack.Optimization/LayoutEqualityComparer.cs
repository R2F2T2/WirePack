using System.Linq;
using WirePack.Core.Geometry;

namespace WirePack.Optimization;

/// <summary>
/// Компаратор для определения геометрической эквивалентности компоновок
/// с учётом переноса, поворота, отражения и перестановки проводов одинакового диаметра.
/// </summary>
public sealed class LayoutEqualityComparer : IEqualityComparer<Layout>
{
    /// <inheritdoc />
    public bool Equals(Layout? x, Layout? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null || y is null)
            return false;
        if (x.Count != y.Count)
            return false;

        // Сравниваем диаметры проводов
        var xDiameters = x.Wires.Select(w => w.Diameter).OrderBy(d => d).ToArray();
        var yDiameters = y.Wires.Select(w => w.Diameter).OrderBy(d => d).ToArray();

        for (int i = 0; i < xDiameters.Length; i++)
        {
            if (!GeometryTolerance.AreClose(xDiameters[i], yDiameters[i]))
                return false;
        }

        // Нормализуем компоновки (центрируем в origem)
        var xNormalized = Normalize(x);
        var yNormalized = Normalize(y);

        return AreCongruent(xNormalized, yNormalized);
    }

    /// <inheritdoc />
    public int GetHashCode(Layout obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        int hash = obj.Count.GetHashCode();
        foreach (double d in obj.Wires.Select(w => w.Diameter).OrderBy(d => d))
        {
            hash = HashCode.Combine(hash, Math.Round(d, 4));
        }
        return hash;
    }

    private static Layout Normalize(Layout layout)
    {
        // Вычисляем центр массы
        double sumX = 0d;
        double sumY = 0d;
        foreach (Wire w in layout.Wires)
        {
            sumX += w.Center.X;
            sumY += w.Center.Y;
        }

        double centerCount = layout.Count > 0 ? layout.Count : 1;
        var center = new Point(sumX / centerCount, sumY / centerCount);

        // Переносим компоновку так, чтобы её центр оказался в'origine
        var translated = layout.Translate(center.X * -1, center.Y * -1);
        return translated;
    }

    private static bool AreCongruent(Layout a, Layout b)
    {
        if (a.Count == 0)
            return true;
        if (a.Count == 1)
        {
            var aWire = a.Wires[0];
            var bWire = b.Wires[0];
            return GeometryTolerance.AreClose(aWire.Center.DistanceTo(Point.Origin), bWire.Center.DistanceTo(Point.Origin)) &&
                   GeometryTolerance.AreClose(aWire.Diameter, bWire.Diameter);
        }

        var aPoints = a.Wires.Select(w => (Center: w.Center, Diameter: w.Diameter)).ToArray();
        var bPoints = b.Wires.Select(w => (Center: w.Center, Diameter: w.Diameter)).ToArray();

        // Сортируем по диаметру, затем по расстоянию от origine
        Array.Sort(aPoints, (p1, p2) =>
        {
            int cmp = p1.Diameter.CompareTo(p2.Diameter);
            if (cmp != 0) return cmp;
            return p1.Center.DistanceTo(Point.Origin).CompareTo(p2.Center.DistanceTo(Point.Origin));
        });
        Array.Sort(bPoints, (p1, p2) =>
        {
            int cmp = p1.Diameter.CompareTo(p2.Diameter);
            if (cmp != 0) return cmp;
            return p1.Center.DistanceTo(Point.Origin).CompareTo(p2.Center.DistanceTo(Point.Origin));
        });

        // Проверяем соответствие диаметров и расстояний отorigine
        for (int i = 0; i < aPoints.Length; i++)
        {
            if (!GeometryTolerance.AreClose(aPoints[i].Diameter, bPoints[i].Diameter))
                return false;
            if (!GeometryTolerance.AreClose(aPoints[i].Center.DistanceTo(Point.Origin), bPoints[i].Center.DistanceTo(Point.Origin)))
                return false;
        }

        // Ищем опорный провод (с ненулевым расстоянием отorigine)
        int refIdx = -1;
        for (int i = 0; i < aPoints.Length; i++)
        {
            if (!GeometryTolerance.IsZero(aPoints[i].Center.DistanceTo(Point.Origin)))
            {
                refIdx = i;
                break;
            }
        }

        if (refIdx == -1)
            return true; // Все провода в центре

        var pA = aPoints[refIdx];
        double angleA = Math.Atan2(pA.Center.Y, pA.Center.X);

        // Пробуем все провода в b как кандидатов на соответствие опорному
        for (int j = 0; j < bPoints.Length; j++)
        {
            var pB = bPoints[j];
            if (!GeometryTolerance.AreClose(pA.Diameter, pB.Diameter))
                continue;
            if (!GeometryTolerance.AreClose(pA.Center.DistanceTo(Point.Origin), pB.Center.DistanceTo(Point.Origin)))
                continue;

            double angleB = Math.Atan2(pB.Center.Y, pB.Center.X);

            // Пробуем без отражения и с отражением
            foreach (bool reflect in new[] { false, true })
            {
                double theta = angleB - (reflect ? -angleA : angleA);
                if (CheckTransformation(aPoints, bPoints, theta, reflect))
                    return true;
            }
        }

        return false;
    }

    private static bool CheckTransformation((Point Center, double Diameter)[] a, (Point Center, double Diameter)[] b, double theta, bool reflect)
    {
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);

        // Пытаемся совпасть каждый провод из a с каким-то проводом из b
        var unmatchedB = new bool[b.Length];
        for (int i = 0; i < b.Length; i++)
            unmatchedB[i] = true;

        for (int i = 0; i < a.Length; i++)
        {
            double x = a[i].Center.X;
            double y = a[i].Center.Y;

            // Применяем трансформацию
            double tx, ty;
            if (reflect)
                y = -y;

            tx = x * cos - y * sin;
            ty = x * sin + y * cos;
            var transformed = new Point(tx, ty);

            bool found = false;
            for (int j = 0; j < b.Length; j++)
            {
                if (!unmatchedB[j]) continue;
                if (!GeometryTolerance.AreClose(a[i].Diameter, b[j].Diameter)) continue;

                // Сопоставляем по расстоянию отorigine после трансформации
                if (transformed.DistanceTo(b[j].Center) <= GeometryTolerance.Eps * 20.0)
                {
                    unmatchedB[j] = false;
                    found = true;
                    break;
                }
            }

            if (!found)
                return false;
        }

        // Все провода b должны быть сопоставлены
        return !unmatchedB.Any(m => m);
    }
}