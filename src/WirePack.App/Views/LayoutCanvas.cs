using System.Globalization;
using Avalonia;
using Avalonia.Media;
using WirePack.App.Models;
using WPGeometry = WirePack.Core.Geometry;

namespace WirePack.App.Views;

/// <summary>
/// Кастомный элемент управления для визуализации трубки и проводов.
/// </summary>
public class LayoutCanvas : Avalonia.Controls.Control
{
    /// <summary>
    /// Свойство зависимостей: вариант компоновки для отображения.
    /// </summary>
    public static readonly StyledProperty<VariantDisplayModel?> VariantProperty =
        AvaloniaProperty.Register<LayoutCanvas, VariantDisplayModel?>(nameof(Variant));

    public VariantDisplayModel? Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    static LayoutCanvas()
    {
        AffectsRender<LayoutCanvas>(VariantProperty);
    }

    public sealed override void Render(DrawingContext context)
    {
        base.Render(context);

        var variant = Variant;
        if (variant is null)
        {
            // Текстовое сообщение покажем в XAML (ListBox), чтобы избежать проблем с FormattedText API
            return;
        }

        DrawLayout(context, variant);
    }

    private void DrawLayout(DrawingContext context, VariantDisplayModel variant)
    {
        double canvasWidth = Bounds.Width;
        double canvasHeight = Bounds.Height;

        double tubeRadius = variant.Tube.InnerRadius;
        if (tubeRadius <= 0d)
            return;

        // Отступ по краям для видимости
        double margin = 20d;
        double availableSize = Math.Min(canvasWidth, canvasHeight) - 2d * margin;
        double scale = availableSize / (2d * tubeRadius);

        double offsetX = canvasWidth / 2d;
        double offsetY = canvasHeight / 2d;

        // 1. Трубка — тонкая окружность
        DrawCircle(context, offsetX, offsetY, tubeRadius * scale, new Pen(new SolidColorBrush(Colors.Black), 1.5));

        // 2. Провода — залитые окружности с контуром
        var wireFill = new SolidColorBrush(Color.FromArgb(60, 80, 160, 240));
        var wireStroke = new SolidColorBrush(Color.FromArgb(200, 40, 80, 160));
        var wirePen = new Pen(wireStroke, 1d);

        // Смещение центра трубки (компоновка может быть не в начале координат)
        double centerDx = variant.Tube.Center.X;
        double centerDy = variant.Tube.Center.Y;

        foreach (WPGeometry.Wire wire in variant.Layout.Wires)
        {
            double cx = offsetX + (wire.Center.X - centerDx) * scale;
            double cy = offsetY + (wire.Center.Y - centerDy) * scale;
            double r = wire.Radius * scale;

            DrawCircle(context, cx, cy, r, wirePen, wireFill);
        }
    }

    private static void DrawCircle(DrawingContext context, double cx, double cy, double radius, Pen pen,
        IBrush? fill = null)
    {
        if (radius <= 0d)
            return;

        if (fill is not null)
            context.DrawEllipse(fill, pen, new Avalonia.Point(cx, cy), radius, radius);
        else
            context.DrawEllipse(null, pen, new Avalonia.Point(cx, cy), radius, radius);
    }
}