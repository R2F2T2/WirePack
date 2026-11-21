namespace WirePack.Core.Geometry;

/// <summary>
/// Независимая проверка геометрической корректности компоновки.
/// </summary>
public static class LayoutValidator
{
    /// <summary>
    /// Проверяет отсутствие пересечений проводов и нахождение всех проводов внутри трубки.
    /// </summary>
    public static LayoutValidationResult Validate(Layout layout, Tube tube)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var issues = new List<ValidationIssue>();

        for (int i = 0; i < layout.Count; i++)
        {
            for (int j = i + 1; j < layout.Count; j++)
            {
                if (GeometryOperations.WiresIntersect(layout[i], layout[j]))
                {
                    issues.Add(ValidationIssue.ForPair(
                        ValidationIssueKind.WireIntersection,
                        i,
                        j,
                        $"Провода {i} и {j} пересекаются."));
                }
            }
        }

        for (int i = 0; i < layout.Count; i++)
        {
            if (!GeometryOperations.WireInsideTube(layout[i], tube))
            {
                issues.Add(ValidationIssue.ForWire(
                    ValidationIssueKind.WireOutsideTube,
                    i,
                    $"Провод {i} выходит за пределы трубки."));
            }
        }

        return issues.Count == 0 ? LayoutValidationResult.Success : new LayoutValidationResult(issues);
    }

    /// <summary>
    /// Пересчитывает MEC по фактическим координатам и проверяет сохранённые центр и радиус трубки.
    /// </summary>
    public static LayoutValidationResult ValidateMec(Layout layout, Tube storedTube)
    {
        ArgumentNullException.ThrowIfNull(layout);
        MecResult expectedMec = NumericalWeightedMecSolver.Solve(layout);
        return ValidateMec(layout, storedTube, expectedMec);
    }

    /// <summary>
    /// Проверяет сохранённые центр и радиус трубки по заранее вычисленному MEC.
    /// </summary>
    public static LayoutValidationResult ValidateMec(Layout layout, Tube storedTube, MecResult expectedMec)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(expectedMec);

        var issues = new List<ValidationIssue>();

        double centerDistance = storedTube.Center.DistanceTo(expectedMec.Center);
        if (!GeometryTolerance.IsZero(centerDistance))
        {
            issues.Add(ValidationIssue.ForWire(
                ValidationIssueKind.MecCenterMismatch,
                -1,
                $"Сохранённый центр трубки не совпадает с центром MEC: расхождение {centerDistance:G17} мм."));
        }

        double diameterDifference = Math.Abs(storedTube.InnerDiameter - expectedMec.Diameter);
        if (!GeometryTolerance.IsZero(diameterDifference))
        {
            issues.Add(ValidationIssue.ForWire(
                ValidationIssueKind.MecDiameterMismatch,
                -1,
                $"Сохранённый диаметр трубки не совпадает с диаметром MEC: расхождение {diameterDifference:G17} мм."));
        }

        return issues.Count == 0 ? LayoutValidationResult.Success : new LayoutValidationResult(issues);
    }

    /// <summary>
    /// Выполняет полную проверку компоновки и сохранённых параметров трубки.
    /// </summary>
    public static LayoutValidationResult ValidateAll(Layout layout, Tube storedTube)
    {
        LayoutValidationResult geometry = Validate(layout, storedTube);
        LayoutValidationResult mec = ValidateMec(layout, storedTube);

        if (geometry.IsValid && mec.IsValid)
            return LayoutValidationResult.Success;

        return new LayoutValidationResult(geometry.Issues.Concat(mec.Issues));
    }
}
