using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace WirePack.App.Models;

/// <summary>
/// Палитра цветов для групп проводов.
/// </summary>
public static class WireGroupPalette
{
    private static readonly Color[] Palette = new[]
    {
        Color.FromRgb(59, 130, 246),   // Blue
        Color.FromRgb(16, 185, 129),  // Emerald
        Color.FromRgb(245, 158, 11),  // Amber
        Color.FromRgb(139, 92, 246),  // Purple
        Color.FromRgb(236, 72, 153),  // Pink
        Color.FromRgb(6, 182, 212),   // Cyan
        Color.FromRgb(249, 115, 22),  // Orange
        Color.FromRgb(99, 102, 241),  // Indigo
        Color.FromRgb(20, 184, 166),  // Teal
        Color.FromRgb(234, 179, 8),   // Yellow
        Color.FromRgb(168, 85, 247),  // Violet
        Color.FromRgb(132, 204, 16)   // Lime
    };

    public static Color GetColor(int index)
    {
        return Palette[index < 0 ? 0 : index % Palette.Length];
    }
}

/// <summary>
/// Одна группа проводов одинакового диаметра.
/// </summary>
public sealed class WireGroupModel : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private double _diameter = 2d;
    private int _count = 1;
    private Color _color = WireGroupPalette.GetColor(0);

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <summary>
    /// Наружный диаметр провода в миллиметрах.
    /// </summary>
    public double Diameter
    {
        get => _diameter;
        set
        {
            if (value > 0d && double.IsFinite(value))
            {
                _diameter = value;
                OnPropertyChanged();
                ClearErrors(nameof(Diameter));
            }
            else
            {
                _diameter = value;
                OnPropertyChanged();
                SetError(nameof(Diameter), "Диаметр должен быть положительным числом.");
            }
        }
    }

    /// <summary>
    /// Количество проводов в группе.
    /// </summary>
    public int Count
    {
        get => _count;
        set
        {
            if (value > 0 && value <= 100)
            {
                _count = value;
                OnPropertyChanged();
                ClearErrors(nameof(Count));
            }
            else
            {
                _count = value;
                OnPropertyChanged();
                SetError(nameof(Count), "Количество должно быть от 1 до 100.");
            }
        }
    }

    /// <summary>
    /// Цвет группы проводов.
    /// </summary>
    public Color Color
    {
        get => _color;
        set
        {
            _color = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(GroupBackgroundBrush));
        }
    }

    /// <summary>
    /// Полупрозрачная кисть фона группы для интерфейса.
    /// </summary>
    public IBrush GroupBackgroundBrush =>
        new SolidColorBrush(Color.FromArgb(40, _color.R, _color.G, _color.B));

    public bool HasErrors => _errors.Count > 0;

    private readonly Dictionary<string, string> _errors = new();

    public System.Collections.IEnumerable GetErrors(string? propertyName)
    {
        if (propertyName is not null && _errors.TryGetValue(propertyName, out var error))
            return new[] { error };
        return Array.Empty<string>();
    }

    private void SetError(string propertyName, string error)
    {
        _errors[propertyName] = error;
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    private void ClearErrors(string propertyName)
    {
        if (_errors.Remove(propertyName))
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}