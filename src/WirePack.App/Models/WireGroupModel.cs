using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WirePack.App.Models;

/// <summary>
/// Одна группа проводов одинакового диаметра.
/// </summary>
public sealed class WireGroupModel : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private double _diameter = 2d;
    private int _count = 1;

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