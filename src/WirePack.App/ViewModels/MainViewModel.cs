using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WirePack.App.Models;
using WirePack.App.Project;
using WirePack.Core.Geometry;
using WirePack.Core.Project;
using WirePack.Optimization;

namespace WirePack.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private const int MaxTotalWires = 100;

    private readonly BundleCompressionOptimizer _optimizer = new();
    private IOptimizationSession? _currentSession;
    private CancellationTokenSource? _stopCts;
    private Timer? _timer;
    private Stopwatch? _stopwatch;
    private string? _projectFilePath;

    public MainViewModel()
    {
        WireGroups.CollectionChanged += (_, _) => UpdateCanStart();
        IsNotRunning = true;
    }

    // ====== Свойства ======

    /// <summary>
    /// Группы проводов для ввода.
    /// </summary>
    public ObservableCollection<WireGroupModel> WireGroups { get; } = new()
    {
        new WireGroupModel { Diameter = 2d, Count = 3 },
    };

    /// <summary>
    /// Найденные варианты компоновок.
    /// </summary>
    public ObservableCollection<VariantDisplayModel> Variants { get; } = new();

    [ObservableProperty]
    private VariantDisplayModel? _selectedVariant;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isNotRunning;

    partial void OnIsRunningChanged(bool value)
    {
        IsNotRunning = !value;
    }

    [ObservableProperty]
    private string _statusText = "Готов к расчёту";

    [ObservableProperty]
    private string _elapsedText = "00:00";

    [ObservableProperty]
    private int _currentIteration;

    [ObservableProperty]
    private int _currentRestartCount;

    [ObservableProperty]
    private double? _currentBestDiameter;

    [ObservableProperty]
    private double _fixedScale; // 0.0 = auto-dynamic, >0 = constant scale for tube visualization

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private int _variantCount;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasVariants;

    [ObservableProperty]
    private bool _isVisualizationEnabled = true;

    [ObservableProperty]
    private bool _isTestDataSet;

    partial void OnIsTestDataSetChanged(bool value)
    {
        if (value)
        {
            WireGroups.Clear();
            WireGroups.Add(new WireGroupModel { Diameter = 2d, Count = 20 });
            WireGroups.Add(new WireGroupModel { Diameter = 3d, Count = 20 });
            WireGroups.Add(new WireGroupModel { Diameter = 4d, Count = 20 });
            WireGroups.Add(new WireGroupModel { Diameter = 5d, Count = 20 });
            WireGroups.Add(new WireGroupModel { Diameter = 6d, Count = 10 });
            WireGroups.Add(new WireGroupModel { Diameter = 7d, Count = 10 });
        }
    }

    // ====== Команды ======

    [RelayCommand]
    private void AddGroup()
    {
        WireGroups.Add(new WireGroupModel { Diameter = 2d, Count = 1 });
    }

    [RelayCommand]
    private void RemoveGroup(WireGroupModel? group)
    {
        if (group is not null && WireGroups.Count > 1)
            WireGroups.Remove(group);
    }

    [RelayCommand]
    private async Task StartCalculation()
    {
        if (IsRunning)
            return;

        // Проверка ошибок в группах
        foreach (var g in WireGroups)
        {
            if (g.HasErrors)
            {
                ErrorMessage = "Исправьте ошибки в вводе данных.";
                return;
            }
        }

        // Формируем провода
        var wireList = new List<Wire>();
        int totalCount = 0;
        foreach (var group in WireGroups)
        {
            int count = group.Count;
            if (totalCount + count > MaxTotalWires)
            {
                ErrorMessage = $"Общее количество проводов не должно превышать {MaxTotalWires}.";
                return;
            }

            for (int i = 0; i < count; i++)
                wireList.Add(new Wire(Point.Origin, group.Diameter));

            totalCount += count;
        }

        if (wireList.Count == 0)
        {
            ErrorMessage = "Добавьте хотя бы один провод.";
            return;
        }

        ErrorMessage = null;
        IsRunning = true;
        StatusText = "Расчёт выполняется...";
        ProgressPercent = 0d;
        CurrentIteration = 0;
        CurrentRestartCount = 0;
        CurrentBestDiameter = null;
        // Устанавливаем постоянный масштаб, чтобы визуализация показывала уменьшение диаметра трубки.
        FixedScale = 20.0; 
        Variants.Clear();
        HasVariants = false;
        SelectedVariant = null;
        VariantCount = 0;
        _stopCts = new CancellationTokenSource();

        // Таймер для отображения времени
        _stopwatch = Stopwatch.StartNew();
        _timer = new Timer(_ =>
        {
            if (_stopwatch is not null)
            {
                ElapsedText = _stopwatch.Elapsed.ToString(@"mm\:ss");
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        try
        {
            var request = new OptimizationRequest(wireList, visualizeSteps: IsVisualizationEnabled);

            _currentSession = _optimizer.Start(request, new Progress<OptimizationProgress>(OnProgress));

            OptimizationResult result = await _currentSession.Completion;

            // Обновляем варианты
            Variants.Clear();
            for (int i = 0; i < result.Variants.Count; i++)
            {
                var lv = result.Variants[i];
                // Создаем Tube из Mec результата
                var tube = new Tube(lv.Mec.Center, lv.Mec.Radius);
                // LayoutVariant уже содержит Layout и Mec, создаем VariantDisplayModel
                var display = new VariantDisplayModel(i + 1, lv.Layout, tube);
                Variants.Add(display);
            }

            VariantCount = Variants.Count;
            HasVariants = Variants.Count > 0;

            if (HasVariants)
                SelectedVariant = Variants[0];

            // Текстовое описание причины остановки
            StatusText = result.StopReason switch
            {
                OptimizationStopReason.SufficientVariantsFound =>
                    $"Найдено {result.Variants.Count} вариантов. Расчёт завершён досрочно.",
                OptimizationStopReason.TimeLimitReached =>
                    $"Достигнут лимит времени. Найдено {result.Variants.Count} вариант.",
                OptimizationStopReason.UserRequested =>
                    $"Расчёт остановлен пользователем. Найдено {result.Variants.Count} вариант.",
                _ => $"Расчёт завершён. Найдено {result.Variants.Count} вариантов."
            };
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка расчёта: {ex.Message}";
            StatusText = "Ошибка расчёта.";
        }
        finally
        {
            _timer?.Dispose();
            _timer = null;
            _stopwatch?.Stop();
            _stopCts?.Dispose();
            _stopCts = null;
            _currentSession = null;
            IsRunning = false;
        }
    }

    [RelayCommand]
    private void StopCalculation()
    {
        if (_currentSession is not null)
        {
            _currentSession.Stop();
            StatusText = "Остановка расчёта...";
        }
    }

    [RelayCommand]
    private async Task SaveProject()
    {
        if (Variants.Count == 0 && WireGroups.Count == 0)
        {
            ErrorMessage = "Нет данных для сохранения.";
            return;
        }

        // Формируем ProjectDocument из групп проводов и вариантов
        // WireGroupDto: Diameter (double), Count (int)
        var wireGroupDtos = WireGroups
            .Where(g => !g.HasErrors)
            .Select(g => new WireGroupDto
            {
                Diameter = g.Diameter,
                Count = g.Count
            })
            .ToArray();

        // LayoutVariantDto: Id (string), TubeDiameter (double), TubeCenter (ProjectPoint), Wires (List<ProjectWireDto>)
        var variantDtos = Variants
            .Select(v =>
            {
                var wires = v.Layout.Wires.Select(w => new ProjectWireDto
                {
                    Diameter = w.Diameter,
                    Center = new ProjectPoint(w.Center.X, w.Center.Y)
                }).ToList();
                var tubeCenter = new ProjectPoint(v.Tube.Center.X, v.Tube.Center.Y);
                return new LayoutVariantDto
                {
                    Id = $"var-{Guid.NewGuid():N}",
                    TubeDiameter = (float)v.Tube.InnerDiameter,
                    TubeCenter = tubeCenter,
                    Wires = wires
                };
            })
            .ToArray();

        var document = new ProjectDocument
        {
            WireGroups = wireGroupDtos.ToList(),
            Variants = variantDtos.ToList()
        };

        if (_projectFilePath is null)
        {
            _projectFilePath = $"WirePack_{DateTime.Now:yyyyMMdd_HHmmss}.wirepack";
        }

        try
        {
            ProjectFileSerializer.Save(_projectFilePath, document);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenProject()
    {
        // В реальном приложении — OpenFileDialog
        var path = _projectFilePath;
        if (path is null)
        {
            return;
        }

        ProjectLoadResult loadResult = ProjectFileSerializer.Load(path);

        if (!loadResult.IsSuccess)
        {
            ErrorMessage = loadResult.ErrorMessage;
            return;
        }

        var document = loadResult.Document!;

        // Загружаем группы
        WireGroups.Clear();
        foreach (var group in document.WireGroups)
        {
            WireGroups.Add(new WireGroupModel
            {
                Diameter = group.Diameter,
                Count = group.Count,
            });
        }

        // Загружаем варианты
        Variants.Clear();
        if (document.Variants is not null)
        {
            int idx = 1;
            foreach (var dto in document.Variants)
            {
                var wires = dto.Wires
                    .Select(w => new Wire(new Point(w.Center.X, w.Center.Y), (double)w.Diameter))
                    .ToArray();
                var layout = new Layout(wires);
                var tube = new Tube(new Point(dto.TubeCenter.X, dto.TubeCenter.Y), (double)dto.TubeDiameter / 2);
                Variants.Add(new VariantDisplayModel(idx++, layout, tube));
            }

            VariantCount = Variants.Count;
            HasVariants = Variants.Count > 0;
            if (HasVariants)
                SelectedVariant = Variants[0];
        }

        ErrorMessage = null;
    }

    // ====== Вспомогательные методы ======

    private async void OnProgress(OptimizationProgress progress)
    {
        ProgressPercent = Math.Min(progress.Iteration % 100, 99);
        VariantCount = progress.VariantCount;
        CurrentIteration = progress.Iteration;
        CurrentRestartCount = progress.RestartCount;
        CurrentBestDiameter = progress.BestDiameter;

        if (IsVisualizationEnabled)
        {
            // Обновление SelectedVariant для рисования текущего состояния LayoutCanvas
            if (progress.CurrentLayout is not null && progress.CurrentTube is not null)
            {
                SelectedVariant = new VariantDisplayModel(
                    index: 1,
                    layout: progress.CurrentLayout,
                    tube: progress.CurrentTube!.Value);
            }
            else
            {
                SelectedVariant = null;
            }

            // Задержка для плавной визуализации размещения и итераций, если они происходят очень быстро
            await Task.Delay(30);
        }
    }

    private void UpdateCanStart()
    {
        StartCalculationCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedVariantChanged(VariantDisplayModel? value)
    {
        // Обновление LayoutCanvas произойдёт через привязку
    }
}