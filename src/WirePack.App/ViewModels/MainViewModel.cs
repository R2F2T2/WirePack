using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Media;
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
    private const string ProjectExtension = ".wpack";

    private readonly BundleCompressionOptimizer _optimizer = new();
    private readonly IProjectFileService _projectFileService;
    private readonly IProjectDialogService? _projectDialogService;
    private IOptimizationSession? _currentSession;
    private CancellationTokenSource? _stopCts;
    private Timer? _timer;
    private Stopwatch? _stopwatch;
    private string? _projectFilePath;
    private bool _suppressDirtyTracking;

    public MainViewModel()
        : this(new ProjectFileService())
    {
    }

    public MainViewModel(
        IProjectFileService projectFileService,
        IProjectDialogService? projectDialogService = null)
    {
        _projectFileService = projectFileService ?? throw new ArgumentNullException(nameof(projectFileService));
        _projectDialogService = projectDialogService;

        WireGroups.CollectionChanged += OnWireGroupsChanged;
        Variants.CollectionChanged += OnVariantsChanged;

        foreach (WireGroupModel group in WireGroups)
            group.PropertyChanged += OnWireGroupPropertyChanged;

        IsNotRunning = true;
    }

    // ====== Свойства ======

    /// <summary>
    /// Группы проводов для ввода.
    /// </summary>
    public ObservableCollection<WireGroupModel> WireGroups { get; } = new()
    {
        new WireGroupModel { Diameter = 2d, Count = 3, Color = WireGroupPalette.GetColor(0) },
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
    private string _currentPhaseText = string.Empty;

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

    /// <summary>
    /// Признак наличия несохранённых изменений проекта.
    /// </summary>
    [ObservableProperty]
    private bool _isDirty;

    partial void OnIsTestDataSetChanged(bool value)
    {
        if (value)
        {
            WireGroups.Clear();
            var testDiameters = new[] { 2d, 3d, 4d, 5d, 6d, 7d };
            var testCounts = new[] { 20, 20, 20, 20, 10, 10 };
            for (int i = 0; i < testDiameters.Length; i++)
            {
                WireGroups.Add(new WireGroupModel
                {
                    Diameter = testDiameters[i],
                    Count = testCounts[i],
                    Color = WireGroupPalette.GetColor(i)
                });
            }
        }
    }

    // ====== Команды ======

    [RelayCommand]
    private void AddGroup()
    {
        var usedColors = WireGroups.Select(g => g.Color).ToList();
        WireGroups.Add(new WireGroupModel
        {
            Diameter = 2d,
            Count = 1,
            Color = WireGroupPalette.GetNextColor(usedColors)
        });
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
        CurrentPhaseText = "Укладка";
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
            var assignments = BuildWireAssignments();
            for (int i = 0; i < result.Variants.Count; i++)
            {
                var lv = result.Variants[i];
                // Создаем Tube из Mec результата
                var tube = new Tube(lv.Mec.Center, lv.Mec.Radius);
                // LayoutVariant уже содержит Layout и Mec, создаем VariantDisplayModel
                var display = new VariantDisplayModel(
                    i + 1,
                    lv.Layout,
                    tube,
                    assignments.Colors,
                    assignments.GroupIds,
                    lv.FillFactor);
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
            CurrentPhaseText = "Ошибка";
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
    private Task SaveProject() => SaveProjectCoreAsync(saveAs: false);

    [RelayCommand]
    private Task SaveProjectAs() => SaveProjectCoreAsync(saveAs: true);

    private async Task<bool> SaveProjectCoreAsync(bool saveAs)
    {
        if (IsRunning)
        {
            ErrorMessage = "Нельзя сохранить проект во время расчёта.";
            return false;
        }

        if (!TryCreateDocument(out ProjectDocument document, out string? validationError))
        {
            ErrorMessage = validationError;
            return false;
        }

        string? path = _projectFilePath;

        if (saveAs || string.IsNullOrWhiteSpace(path))
        {
            if (_projectDialogService is null)
            {
                ErrorMessage = "Диалог сохранения проекта недоступен.";
                return false;
            }

            string? pickedPath = await _projectDialogService.PickSaveProjectPathAsync(path);
            if (string.IsNullOrWhiteSpace(pickedPath))
                return false;

            path = EnsureProjectExtension(pickedPath);
        }

        try
        {
            await _projectFileService.SaveAsync(path, document);
        }
        catch (ProjectFileException exception)
        {
            ErrorMessage = exception.Message;
            return false;
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Ошибка сохранения проекта: {exception.Message}";
            return false;
        }

        _projectFilePath = path;
        IsDirty = false;
        ErrorMessage = null;
        StatusText = "Проект сохранён.";
        return true;
    }

    [RelayCommand]
    private async Task OpenProject()
    {
        if (IsRunning)
        {
            ErrorMessage = "Нельзя открыть проект во время расчёта.";
            return;
        }

        if (_projectDialogService is null)
        {
            ErrorMessage = "Диалог открытия проекта недоступен.";
            return;
        }

        string? path = await _projectDialogService.PickOpenProjectPathAsync();
        if (string.IsNullOrWhiteSpace(path))
            return;

        bool canReplace = await ConfirmDiscardOrSaveAsync("открытие другого проекта");
        if (!canReplace)
            return;

        ProjectLoadResult loadResult;

        try
        {
            loadResult = await _projectFileService.LoadAsync(path);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Ошибка открытия проекта: {exception.Message}";
            return;
        }

        if (!loadResult.IsSuccess || loadResult.Document is null)
        {
            ErrorMessage = loadResult.ErrorMessage ?? "Не удалось открыть проект.";
            return;
        }

        ApplyDocument(loadResult.Document);
        _projectFilePath = path;
        IsDirty = false;
        ErrorMessage = null;
        StatusText = loadResult.Issues.Count > 0
            ? $"Проект открыт. Часть результатов отброшена: замечаний — {loadResult.Issues.Count}."
            : "Проект открыт.";
    }

    /// <summary>
    /// Подтверждает закрытие окна при наличии несохранённых изменений.
    /// </summary>
    public async Task<bool> ConfirmCloseAsync()
    {
        if (!IsDirty)
            return true;

        return await ConfirmDiscardOrSaveAsync("закрытие приложения");
    }

    private async Task<bool> ConfirmDiscardOrSaveAsync(string actionDescription)
    {
        if (!IsDirty)
            return true;

        if (_projectDialogService is null)
            return false;

        UnsavedChangesChoice choice = await _projectDialogService
            .ConfirmUnsavedChangesAsync(actionDescription);

        return choice switch
        {
            UnsavedChangesChoice.Save => await SaveProjectCoreAsync(saveAs: false),
            UnsavedChangesChoice.Discard => true,
            _ => false
        };
    }

    private bool TryCreateDocument(
        out ProjectDocument document,
        out string? validationError)
    {
        document = null!;
        validationError = null;

        if (WireGroups.Count == 0)
        {
            validationError = "Добавьте хотя бы одну группу проводов.";
            return false;
        }

        foreach (WireGroupModel group in WireGroups)
        {
            if (group.HasErrors)
            {
                validationError = "Исправьте ошибки в данных групп проводов.";
                return false;
            }
        }

        long totalCount = 0;
        foreach (WireGroupModel group in WireGroups)
            totalCount += group.Count;

        if (totalCount > MaxTotalWires)
        {
            validationError = $"Общее количество проводов не должно превышать {MaxTotalWires}.";
            return false;
        }

        document = ProjectDocumentMapper.CreateDocument(WireGroups, Variants);
        return true;
    }

    private void ApplyDocument(ProjectDocument document)
    {
        _suppressDirtyTracking = true;

        try
        {
            WireGroups.Clear();
            foreach (WireGroupModel group in ProjectDocumentMapper.CreateWireGroups(document))
                WireGroups.Add(group);

            Variants.Clear();
            foreach (VariantDisplayModel variant in ProjectDocumentMapper.CreateVariants(document, WireGroups))
                Variants.Add(variant);

            VariantCount = Variants.Count;
            HasVariants = Variants.Count > 0;
            SelectedVariant = HasVariants ? Variants[0] : null;
        }
        finally
        {
            _suppressDirtyTracking = false;
        }
    }

    // ====== Вспомогательные методы ======

    private (IReadOnlyList<Color> Colors, IReadOnlyList<string> GroupIds) BuildWireAssignments()
    {
        var colors = new List<Color>();
        var groupIds = new List<string>();

        foreach (var group in WireGroups)
        {
            for (int i = 0; i < group.Count; i++)
            {
                colors.Add(group.Color);
                groupIds.Add(group.Id);
            }
        }

        return (colors, groupIds);
    }

    private static string EnsureProjectExtension(string path)
    {
        if (string.Equals(Path.GetExtension(path), ProjectExtension, StringComparison.OrdinalIgnoreCase))
            return path;

        return Path.ChangeExtension(path, ProjectExtension);
    }

    private void OnWireGroupsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (WireGroupModel group in e.OldItems)
                group.PropertyChanged -= OnWireGroupPropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (WireGroupModel group in e.NewItems)
                group.PropertyChanged += OnWireGroupPropertyChanged;
        }

        UpdateCanStart();
        MarkDirty();
    }

    private void OnWireGroupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WireGroupModel.Diameter)
            or nameof(WireGroupModel.Count)
            or nameof(WireGroupModel.Color))
        {
            MarkDirty();
        }
    }

    private void OnVariantsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        MarkDirty();
    }

    private void MarkDirty()
    {
        if (_suppressDirtyTracking)
            return;

        IsDirty = true;
    }

    [RelayCommand]
    private void ShowAbout()
    {
        // Вызов окна "О программе" обрабатывается во View или через событие/сервис, 
        // либо окно может показываться из MainWindow при срабатывании события.
        // Передадим событие или вызовем напрямую, если есть обработчик.
    }

    private async void OnProgress(OptimizationProgress progress)
    {
        ProgressPercent = Math.Min(progress.Iteration % 100, 99);
        VariantCount = progress.VariantCount;
        CurrentIteration = progress.Iteration;
        CurrentRestartCount = progress.RestartCount;
        CurrentBestDiameter = progress.BestDiameter;
        CurrentPhaseText = progress.Phase switch
        {
            OptimizationPhase.Placement => "Укладка",
            OptimizationPhase.Compression => "Сжатие",
            OptimizationPhase.Completed => "Готово",
            _ => CurrentPhaseText
        };

        if (IsVisualizationEnabled)
        {
            // Обновление SelectedVariant для рисования текущего состояния LayoutCanvas
            if (progress.CurrentLayout is not null && progress.CurrentTube is not null)
            {
                var assignments = BuildWireAssignments();
                SelectedVariant = new VariantDisplayModel(
                    index: 1,
                    layout: progress.CurrentLayout,
                    tube: progress.CurrentTube!.Value,
                    wireColors: assignments.Colors,
                    wireGroupIds: assignments.GroupIds);
            }
            else
            {
                SelectedVariant = null;
            }

            // Задержка для плавной визуализации размещения и итераций, если они происходят очень быстро
            await Task.Delay(5);
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
