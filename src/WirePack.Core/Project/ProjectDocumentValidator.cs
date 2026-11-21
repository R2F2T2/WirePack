using WirePack.Core.Geometry;

namespace WirePack.Core.Project;

/// <summary>
/// Проверка корректности данных проекта и сохранённых результатов расчёта.
/// </summary>
public static class ProjectDocumentValidator
{
    /// <summary>
    /// Проверяет версию формата, исходные данные и варианты компоновки.
    /// </summary>
    public static ProjectValidationResult Validate(ProjectDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var issues = new List<ProjectValidationIssue>();

        if (!ProjectFormat.IsSupportedVersion(document.FormatVersion))
        {
            issues.Add(new ProjectValidationIssue(
                ProjectValidationIssueKind.UnsupportedFormatVersion,
                ProjectValidationSeverity.Error,
                $"Неподдерживаемая версия формата проекта: {document.FormatVersion}. " +
                $"Поддерживается версия {ProjectFormat.CurrentVersion}."));
        }

        bool wireGroupsValid = ValidateWireGroups(document.WireGroups, issues);
        ValidateVariants(document.WireGroups, wireGroupsValid, document.Variants, issues);

        return issues.Count == 0
            ? ProjectValidationResult.Success
            : new ProjectValidationResult(issues);
    }

    private static bool ValidateWireGroups(
        IReadOnlyList<WireGroupDto>? groups,
        List<ProjectValidationIssue> issues)
    {
        if (groups is null)
        {
            issues.Add(new ProjectValidationIssue(
                ProjectValidationIssueKind.MissingWireGroups,
                ProjectValidationSeverity.Error,
                "Проект не содержит исходных групп проводов."));
            return false;
        }

        bool isValid = true;
        long totalCount = 0;

        for (int i = 0; i < groups.Count; i++)
        {
            WireGroupDto? group = groups[i];
            if (group is null)
            {
                issues.Add(new ProjectValidationIssue(
                    ProjectValidationIssueKind.InvalidWireGroup,
                    ProjectValidationSeverity.Error,
                    $"Описание группы проводов {i + 1} отсутствует."));
                isValid = false;
                continue;
            }

            if (!IsPositiveFinite(group.Diameter))
            {
                issues.Add(new ProjectValidationIssue(
                    ProjectValidationIssueKind.InvalidWireGroupDiameter,
                    ProjectValidationSeverity.Error,
                    $"Диаметр группы проводов {i + 1} должен быть положительным конечным числом."));
                isValid = false;
            }

            if (group.Count <= 0)
            {
                issues.Add(new ProjectValidationIssue(
                    ProjectValidationIssueKind.InvalidWireGroupCount,
                    ProjectValidationSeverity.Error,
                    $"Количество проводов в группе {i + 1} должно быть положительным."));
                isValid = false;
            }
            else
            {
                totalCount += group.Count;
            }
        }

        if (totalCount > ProjectFormat.MaxWireCount)
        {
            issues.Add(new ProjectValidationIssue(
                ProjectValidationIssueKind.WireCountExceeded,
                ProjectValidationSeverity.Error,
                $"Общее количество проводов {totalCount} превышает допустимые {ProjectFormat.MaxWireCount}."));
            isValid = false;
        }

        return isValid;
    }

    private static void ValidateVariants(
        IReadOnlyList<WireGroupDto>? groups,
        bool wireGroupsValid,
        IReadOnlyList<LayoutVariantDto>? variants,
        List<ProjectValidationIssue> issues)
    {
        if (variants is null || variants.Count == 0)
            return;

        var idCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (LayoutVariantDto? variant in variants)
        {
            if (variant is null || string.IsNullOrWhiteSpace(variant.Id))
                continue;

            idCounts.TryGetValue(variant.Id, out int count);
            idCounts[variant.Id] = count + 1;
        }

        for (int variantIndex = 0; variantIndex < variants.Count; variantIndex++)
        {
            LayoutVariantDto? variant = variants[variantIndex];
            int variantNumber = variantIndex + 1;

            if (variant is null)
            {
                issues.Add(new ProjectValidationIssue(
                    ProjectValidationIssueKind.InvalidVariant,
                    ProjectValidationSeverity.Error,
                    $"Описание варианта {variantNumber} отсутствует.",
                    variantIndex));
                continue;
            }

            string? variantId = string.IsNullOrWhiteSpace(variant.Id) ? null : variant.Id;

            if (variantId is null)
            {
                issues.Add(new ProjectValidationIssue(
                    ProjectValidationIssueKind.MissingVariantId,
                    ProjectValidationSeverity.Error,
                    $"У варианта {variantNumber} отсутствует идентификатор.",
                    variantIndex));
            }
            else if (idCounts[variantId] > 1)
            {
                issues.Add(new ProjectValidationIssue(
                    ProjectValidationIssueKind.DuplicateVariantId,
                    ProjectValidationSeverity.Error,
                    $"Идентификатор варианта «{variantId}» повторяется.",
                    variantIndex,
                    variantId));
            }

            bool tubeValid = IsPositiveFinite(variant.TubeDiameter);
            if (!tubeValid)
            {
                issues.Add(new ProjectValidationIssue(
                    ProjectValidationIssueKind.InvalidTubeDiameter,
                    ProjectValidationSeverity.Error,
                    $"Внутренний диаметр трубки варианта {variantNumber} должен быть положительным конечным числом.",
                    variantIndex,
                    variantId));
            }

            bool tubeCenterValid = IsFinite(variant.TubeCenter);
            if (!tubeCenterValid)
            {
                issues.Add(new ProjectValidationIssue(
                    ProjectValidationIssueKind.InvalidCoordinates,
                    ProjectValidationSeverity.Error,
                    $"Координаты центра трубки варианта {variantNumber} должны быть конечными числами.",
                    variantIndex,
                    variantId));
            }

            bool wiresValid = TryCreateWires(variant.Wires, variantNumber, variantIndex, variantId, issues);
            if (!wiresValid)
            {
                if (variant.Wires is null || variant.Wires.Count == 0)
                {
                    issues.Add(new ProjectValidationIssue(
                        ProjectValidationIssueKind.InvalidWireSet,
                        ProjectValidationSeverity.Error,
                        $"Вариант {variantNumber} не содержит проводов.",
                        variantIndex,
                        variantId));
                }
            }
            else if (wireGroupsValid && groups is not null &&
                     !WiresMatchGroups(groups, variant.Wires!))
            {
                issues.Add(new ProjectValidationIssue(
                    ProjectValidationIssueKind.OutdatedResults,
                    ProjectValidationSeverity.Warning,
                    $"Результаты варианта {variantNumber} не соответствуют текущему набору проводов и считаются устаревшими.",
                    variantIndex,
                    variantId));
            }

            if (!tubeValid || !tubeCenterValid || !wiresValid)
                continue;

            var layout = new Layout(variant.Wires!.Select(wire => new Wire(
                new Point(wire.Center.X, wire.Center.Y),
                wire.Diameter)));
            var tube = new Tube(
                new Point(variant.TubeCenter.X, variant.TubeCenter.Y),
                variant.TubeDiameter / 2d);

            LayoutValidationResult geometry = LayoutValidator.ValidateAll(layout, tube);
            foreach (ValidationIssue geometryIssue in geometry.Issues)
            {
                issues.Add(new ProjectValidationIssue(
                    MapGeometryIssue(geometryIssue.Kind),
                    ProjectValidationSeverity.Error,
                    $"Вариант {variantNumber}: {geometryIssue.Message}",
                    variantIndex,
                    variantId));
            }
        }
    }

    private static bool TryCreateWires(
        IReadOnlyList<ProjectWireDto>? wires,
        int variantNumber,
        int variantIndex,
        string? variantId,
        List<ProjectValidationIssue> issues)
    {
        if (wires is null || wires.Count == 0)
            return false;

        bool isValid = true;

        for (int wireIndex = 0; wireIndex < wires.Count; wireIndex++)
        {
            ProjectWireDto? wire = wires[wireIndex];
            if (wire is null)
            {
                issues.Add(new ProjectValidationIssue(
                    ProjectValidationIssueKind.InvalidWireSet,
                    ProjectValidationSeverity.Error,
                    $"Описание провода {wireIndex + 1} варианта {variantNumber} отсутствует.",
                    variantIndex,
                    variantId));
                isValid = false;
                continue;
            }

            if (!IsPositiveFinite(wire.Diameter))
            {
                issues.Add(new ProjectValidationIssue(
                    ProjectValidationIssueKind.InvalidWireDiameter,
                    ProjectValidationSeverity.Error,
                    $"Диаметр провода {wireIndex + 1} варианта {variantNumber} должен быть положительным конечным числом.",
                    variantIndex,
                    variantId));
                isValid = false;
            }

            if (!IsFinite(wire.Center))
            {
                issues.Add(new ProjectValidationIssue(
                    ProjectValidationIssueKind.InvalidCoordinates,
                    ProjectValidationSeverity.Error,
                    $"Координаты центра провода {wireIndex + 1} варианта {variantNumber} должны быть конечными числами.",
                    variantIndex,
                    variantId));
                isValid = false;
            }
        }

        return isValid;
    }

    private static bool WiresMatchGroups(
        IReadOnlyList<WireGroupDto> groups,
        IReadOnlyList<ProjectWireDto> wires)
    {
        var remainingDiameters = new List<double>();

        foreach (WireGroupDto group in groups)
        {
            for (int i = 0; i < group.Count; i++)
                remainingDiameters.Add(group.Diameter);
        }

        if (remainingDiameters.Count != wires.Count)
            return false;

        foreach (ProjectWireDto wire in wires)
        {
            int index = remainingDiameters.FindIndex(
                diameter => GeometryTolerance.AreClose(diameter, wire.Diameter));

            if (index < 0)
                return false;

            remainingDiameters.RemoveAt(index);
        }

        return remainingDiameters.Count == 0;
    }

    private static ProjectValidationIssueKind MapGeometryIssue(ValidationIssueKind kind) =>
        kind switch
        {
            ValidationIssueKind.WireIntersection => ProjectValidationIssueKind.InvalidVariantGeometry,
            ValidationIssueKind.WireOutsideTube => ProjectValidationIssueKind.InvalidVariantGeometry,
            ValidationIssueKind.MecCenterMismatch => ProjectValidationIssueKind.MecCenterMismatch,
            ValidationIssueKind.MecDiameterMismatch => ProjectValidationIssueKind.MecDiameterMismatch,
            _ => ProjectValidationIssueKind.InvalidVariantGeometry
        };

    private static bool IsPositiveFinite(double value) => double.IsFinite(value) && value > 0d;

    private static bool IsFinite(ProjectPoint point) =>
        double.IsFinite(point.X) && double.IsFinite(point.Y);
}
