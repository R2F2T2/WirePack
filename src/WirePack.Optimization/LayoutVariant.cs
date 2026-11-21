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
}
