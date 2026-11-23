using Avalonia.Media;
using WirePack.Core.Geometry;

namespace WirePack.App.Models;

/// <summary>
/// Модель отображения варианта компоновки.
/// </summary>
public sealed class VariantDisplayModel
{
    public VariantDisplayModel(
        int index,
        Layout layout,
        Tube tube,
        IReadOnlyList<Color>? wireColors = null,
        IReadOnlyList<string>? wireGroupIds = null,
        double fillFactor = 0d,
        string? id = null)
    {
        Index = index;
        Layout = layout;
        Tube = tube;
        WireColors = wireColors;
        WireGroupIds = wireGroupIds ?? Array.Empty<string>();
        FillFactor = fillFactor;
        Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
    }

    /// <summary>
    /// Уникальный идентификатор варианта.
    /// </summary>
    public string Id { get; }

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
    /// Идентификаторы групп, соответствующие проводам в Layout.
    /// </summary>
    public IReadOnlyList<string> WireGroupIds { get; }

    /// <summary>
    /// Внутренний диаметр трубки в миллиметрах.
    /// </summary>
    public double Diameter => Tube.InnerDiameter;

    /// <summary>
    /// Коэффициент заполняемости, сохранённый в проекте.
    /// </summary>
    public double FillFactor { get; }

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
