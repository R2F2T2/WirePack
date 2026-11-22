using WirePack.Core.Geometry;

namespace WirePack.Optimization;

/// <summary>
/// Допустимый вариант компоновки вместе с пересчитанной охватывающей окружностью.
/// </summary>
/// <remarks>
/// Диаметр всегда вычисляется через взвешенную MEC по фактическим координатам проводов,
/// а не берётся из промежуточного состояния релаксации.
/// </remarks>
public sealed class LayoutVariant
{
    /// <summary>
    /// Создаёт вариант компоновки и вычисляет для него MEC.
    /// </summary>
    public LayoutVariant(Layout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        if (layout.Count == 0)
            throw new ArgumentException("Компоновка не должна быть пустой.", nameof(layout));

        Layout = layout;
        Mec = NumericalWeightedMecSolver.Solve(layout);
        Tube = new Tube(Mec.Center, Mec.Radius);
    }

    /// <summary>
    /// Координаты центров проводов.
    /// </summary>
    public Layout Layout { get; }

    /// <summary>
    /// Минимальная охватывающая окружность, пересчитанная по координатам.
    /// </summary>
    public MecResult Mec { get; }

    /// <summary>
    /// Трубка с центром и радиусом MEC.
    /// </summary>
    public Tube Tube { get; }

    /// <summary>
    /// Внутренний диаметр трубки.
    /// </summary>
    public double Diameter => Tube.InnerDiameter;

    /// <summary>
    /// Коэффициент заполняемости: отношение суммарной площади сечения проводов
    /// к площади внутреннего сечения трубки.
    /// </summary>
    public double FillFactor
    {
        get
        {
            double wireAreaSum = 0d;
            for (int i = 0; i < Layout.Count; i++)
            {
                double radius = Layout[i].Radius;
                wireAreaSum += radius * radius;
            }

            double tubeRadius = Tube.InnerRadius;
            return tubeRadius <= 0d ? 0d : wireAreaSum / (tubeRadius * tubeRadius);
        }
    }

    /// <summary>
    /// Коэффициент заполняемости в процентах.
    /// </summary>
    public double FillFactorPercent => FillFactor * 100d;
}
