using System.Text.Json;
using WirePack.Core.Project;

namespace WirePack.App.Project;

/// <summary>
/// Преобразование проекта WirePack в JSON и обратно.
/// </summary>
public static class ProjectFileSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Сериализует проект в JSON.
    /// </summary>
    public static string Serialize(ProjectDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return JsonSerializer.Serialize(document, SerializerOptions);
    }

    /// <summary>
    /// Открывает проект из JSON с проверкой версии и корректности данных.
    /// </summary>
    /// <remarks>
    /// Некорректные варианты отбрасываются, чтобы они не отображались как актуальные результаты.
    /// Ошибки уровня исходных данных делают открытие проекта неуспешным.
    /// </remarks>
    public static ProjectLoadResult Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return ProjectLoadResult.Failure("Файл проекта пуст.");

        ProjectDocument? document;

        try
        {
            document = JsonSerializer.Deserialize<ProjectDocument>(json, SerializerOptions);
        }
        catch (JsonException exception)
        {
            return ProjectLoadResult.Failure($"Файл проекта повреждён: {exception.Message}");
        }
        catch (NotSupportedException exception)
        {
            return ProjectLoadResult.Failure($"Формат файла проекта не поддерживается: {exception.Message}");
        }

        if (document is null)
            return ProjectLoadResult.Failure("Файл проекта не содержит данных.");

        return ValidateAndSanitize(document);
    }

    private static ProjectLoadResult ValidateAndSanitize(ProjectDocument document)
    {
        ProjectValidationResult validation = ProjectDocumentValidator.Validate(document);

        if (validation.HasFatalErrors)
            return ProjectLoadResult.Failure("Файл проекта содержит некорректные исходные данные.", validation.Issues);

        IReadOnlyList<LayoutVariantDto> variants = document.Variants is null
            ? Array.Empty<LayoutVariantDto>()
            : document.Variants;

        var rejectedIndices = new HashSet<int>();
        foreach (ProjectValidationIssue issue in validation.Issues)
        {
            if (issue.VariantIndex is int variantIndex &&
                (issue.Severity == ProjectValidationSeverity.Error ||
                 issue.Kind == ProjectValidationIssueKind.OutdatedResults))
            {
                rejectedIndices.Add(variantIndex);
            }
        }

        if (rejectedIndices.Count == 0)
            return ProjectLoadResult.Success(document, validation.Issues);

        ProjectDocument sanitized = document.WithVariants(
            variants.Where((_, index) => !rejectedIndices.Contains(index)));

        return ProjectLoadResult.Success(sanitized, validation.Issues);
    }
}
