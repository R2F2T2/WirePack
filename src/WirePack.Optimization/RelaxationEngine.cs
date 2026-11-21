using WirePack.Core.Geometry;

namespace WirePack.Optimization;

/// <summary>
/// Движок квазистатической релаксации компоновки проводов.
/// </summary>
public static class RelaxationEngine
{
    private const int MaxIterations = 300;
    private const double RelaxationFactor = 0.3d;
    private const double MaxDisplacementPerIteration = 0.5d;

    /// <summary>
    /// Выполняет квазистатическую релаксацию компоновки внутри заданного радиуса трубки.
    /// </summary>
    /// <param name="layout">Исходная компоновка.</param>
    /// <param name="targetRadius">Целевой внутренний радиус трубки (центр в начале координат).</param>
    /// <returns>Релаксированная компоновка и флаг успешности (отсутствие пересечений и вхождение в радиус).</returns>
    public static (Layout Layout, bool IsSuccessful) Relax(Layout layout, double targetRadius)
    {
        ArgumentNullException.ThrowIfNull(layout);

        int count = layout.Count;
        var centers = new Point[count];
        for (int i = 0; i < count; i++)
            centers[i] = layout[i].Center;

        var wires = layout.Wires.ToArray();

        for (int iter = 0; iter < MaxIterations; iter++)
        {
            bool hasViolations = false;
            var shifts = new Point[count];

            // 1. Расталкивание пересекающихся проводов
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    var c1 = centers[i];
                    var c2 = centers[j];
                    double dist = c1.DistanceTo(c2);
                    double minDist = wires[i].Radius + wires[j].Radius;

                    if (dist < minDist)
                    {
                        hasViolations = true;
                        double overlap = minDist - dist;
                        if (overlap > GeometryTolerance.Eps)
                        {
                            double dx = c2.X - c1.X;
                            double dy = c2.Y - c1.Y;
                            if (dist < 1e-9)
                            {
                                dx = 1.0;
                                dy = 0.0;
                                dist = 1.0;
                            }

                            double nx = dx / dist;
                            double ny = dy / dist;
                            double shiftMagnitude = overlap * 0.5d * RelaxationFactor;
                            shiftMagnitude = Math.Min(shiftMagnitude, MaxDisplacementPerIteration);

                            shifts[i] = new Point(shifts[i].X - nx * shiftMagnitude, shifts[i].Y - ny * shiftMagnitude);
                            shifts[j] = new Point(shifts[j].X + nx * shiftMagnitude, shifts[j].Y + ny * shiftMagnitude);
                        }
                    }
                }
            }

            // 2. Втягивание проводов, выходящих за целевой радиус трубки
            double allowedRadius = Math.Max(0d, targetRadius);
            for (int i = 0; i < count; i++)
            {
                double distToOrigin = centers[i].DistanceTo(Point.Origin);
                double maxAllowedCenterDist = allowedRadius - wires[i].Radius;

                if (distToOrigin > maxAllowedCenterDist + GeometryTolerance.Eps)
                {
                    hasViolations = true;
                    double excess = distToOrigin - maxAllowedCenterDist;
                    double x = centers[i].X;
                    double y = centers[i].Y;

                    double nx = distToOrigin > 1e-9 ? x / distToOrigin : 1.0;
                    double ny = distToOrigin > 1e-9 ? y / distToOrigin : 0.0;

                    double shiftMagnitude = excess * RelaxationFactor;
                    shiftMagnitude = Math.Min(shiftMagnitude, MaxDisplacementPerIteration);

                    shifts[i] = new Point(shifts[i].X - nx * shiftMagnitude, shifts[i].Y - ny * shiftMagnitude);
                }
            }

            if (!hasViolations)
            {
                // Все условия выполнены
                break;
            }

            // Применяем смещения
            for (int i = 0; i < count; i++)
            {
                double sx = shifts[i].X;
                double sy = shifts[i].Y;
                double mag = Math.Sqrt(sx * sx + sy * sy);
                if (mag > MaxDisplacementPerIteration)
                {
                    sx = sx / mag * MaxDisplacementPerIteration;
                    sy = sy / mag * MaxDisplacementPerIteration;
                }

                centers[i] = new Point(centers[i].X + sx, centers[i].Y + sy);
            }
        }

        var relaxedWires = new Wire[count];
        for (int i = 0; i < count; i++)
        {
            relaxedWires[i] = wires[i].MoveTo(centers[i]);
        }

        var relaxedLayout = new Layout(relaxedWires);
        var tube = new Tube(Point.Origin, targetRadius);
        var validation = LayoutValidator.Validate(relaxedLayout, tube);

        return (relaxedLayout, validation.IsValid);
    }
}
