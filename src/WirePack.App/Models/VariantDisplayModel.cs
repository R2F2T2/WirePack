using WirePack.Core.Geometry;

namespace WirePack.App.Models;

/// <summary>
/// Модель отображения варианта компоновки.
/// </summary>
public sealed class VariantDisplayModel
{
    public VariantDisplayModel(int index, Layout layout, Tube tube)
    {
        Index = index;
        Layout = layout;
        Tube = tube;
    }

    /// <summary>
    /// Номер варианта (1-based).
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Компоновка проводов.
    /// </summary>
    public Layout Layout { get; }

    /// <summary>
    /// Трубка с центром и радиусом MEC.
    /// </summary>
    public Tube Tube { get; }

    /// <summary>
    /// Внутренний диаметр трубки в миллиметрах.
    /// </summary>
    public double Diameter => Tube.InnerDiameter;

    /// <summary>
    /// Текстовое описание для вывода.
    /// </summary>
    public string Description => $"Вариант {Index}: ∅ {Diameter:F2} мм";
}