using Avalonia.Media;
using WirePack.Core.Geometry;

namespace WirePack.App.Models;

/// <summary>
/// Модель отображения варианта компоновки.
/// </summary>
public sealed class VariantDisplayModel
{
    public VariantDisplayModel(int index, Layout layout, Tube tube, IReadOnlyList<Color>? wireColors = null)
    {
        Index = index;
        Layout = layout;
        Tube = tube;
        WireColors = wireColors;
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
    /// Цвета проводов, соответствующие порядку в Layout.
    /// </summary>
    public IReadOnlyList<Color>? WireColors { get; }

    /// <summary>
    /// Внутренний диаметр трубки в миллиметрах.
    /// </summary>
    public double Diameter => Tube.InnerDiameter;

    /// <summary>
    /// Коэффициент заполняемости: отношение суммарной площади проводов
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

    /// <summary>
    /// Текстовое описание для вывода.
    /// </summary>
    public string Description =>
        $"Вариант {Index}: ∅ {Diameter:F2} мм, заполнение {FillFactorPercent:F1}%";
}