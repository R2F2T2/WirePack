using Avalonia.Media;

namespace WirePack.App.Project;

/// <summary>
/// Преобразование цвета группы в строку проекта и обратно.
/// </summary>
public static class ProjectColorConverter
{
    /// <summary>
    /// Преобразует цвет в строку #AARRGGBB.
    /// </summary>
    public static string ToHex(Color color) =>
        $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>
    /// Разбирает строку цвета проекта. При ошибке возвращает запасной цвет.
    /// </summary>
    public static Color FromHex(string? value, Color fallback) =>
        !string.IsNullOrWhiteSpace(value) && Color.TryParse(value, out Color color)
            ? color
            : fallback;
}
