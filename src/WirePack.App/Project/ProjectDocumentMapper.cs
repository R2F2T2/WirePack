using Avalonia.Media;
using WirePack.App.Models;
using WirePack.Core.Geometry;
using WirePack.Core.Project;

namespace WirePack.App.Project;

/// <summary>
/// Преобразование между моделями отображения и моделью проекта.
/// </summary>
public static class ProjectDocumentMapper
{
    /// <summary>
    /// Формирует документ проекта из текущего состояния приложения.
    /// </summary>
    public static ProjectDocument CreateDocument(
        IReadOnlyList<WireGroupModel> groups,
        IReadOnlyList<VariantDisplayModel> variants)
    {
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(variants);

        var groupDtos = groups
            .Select(group => new WireGroupDto
            {
                Id = group.Id,
                Diameter = group.Diameter,
                Count = group.Count,
                Color = ProjectColorConverter.ToHex(group.Color)
            })
            .ToList();

        var variantDtos = variants
            .Select(variant => new LayoutVariantDto
            {
                Id = variant.Id,
                TubeDiameter = variant.Tube.InnerDiameter,
                TubeCenter = new ProjectPoint(variant.Tube.Center.X, variant.Tube.Center.Y),
                FillFactor = variant.FillFactor,
                Wires = variant.Layout.Wires
                    .Select((wire, index) => new ProjectWireDto
                    {
                        GroupId = index < variant.WireGroupIds.Count
                            ? variant.WireGroupIds[index]
                            : string.Empty,
                        Diameter = wire.Diameter,
                        Center = new ProjectPoint(wire.Center.X, wire.Center.Y)
                    })
                    .ToList()
            })
            .ToList();

        return new ProjectDocument
        {
            SchemaVersion = ProjectFormat.CurrentVersion,
            WireGroups = groupDtos,
            Variants = variantDtos
        };
    }

    /// <summary>
    /// Создаёт группы отображения по документу проекта.
    /// </summary>
    public static List<WireGroupModel> CreateWireGroups(ProjectDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var groups = new List<WireGroupModel>(document.WireGroups.Count);

        for (int index = 0; index < document.WireGroups.Count; index++)
        {
            WireGroupDto group = document.WireGroups[index];
            Color fallback = WireGroupPalette.GetColor(index);

            groups.Add(new WireGroupModel
            {
                Id = group.Id,
                Diameter = group.Diameter,
                Count = group.Count,
                Color = ProjectColorConverter.FromHex(group.Color, fallback)
            });
        }

        return groups;
    }

    /// <summary>
    /// Создаёт варианты отображения по документу проекта.
    /// </summary>
    public static List<VariantDisplayModel> CreateVariants(
        ProjectDocument document,
        IReadOnlyList<WireGroupModel> groups)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(groups);

        var colorsByGroup = groups.ToDictionary(
            group => group.Id,
            group => group.Color,
            StringComparer.Ordinal);

        var variants = new List<VariantDisplayModel>(document.Variants.Count);

        for (int index = 0; index < document.Variants.Count; index++)
        {
            LayoutVariantDto variant = document.Variants[index];
            var wires = variant.Wires
                .Select(wire => new Wire(
                    new Point(wire.Center.X, wire.Center.Y),
                    wire.Diameter))
                .ToArray();
            var groupIds = variant.Wires
                .Select(wire => wire.GroupId)
                .ToArray();
            var wireColors = groupIds
                .Select(groupId => colorsByGroup.TryGetValue(groupId, out Color color)
                    ? color
                    : WireGroupPalette.GetColor(0))
                .ToArray();

            var layout = new Layout(wires);
            var tube = new Tube(
                new Point(variant.TubeCenter.X, variant.TubeCenter.Y),
                variant.TubeDiameter / 2d);

            variants.Add(new VariantDisplayModel(
                index + 1,
                layout,
                tube,
                wireColors,
                groupIds,
                variant.FillFactor,
                variant.Id));
        }

        return variants;
    }
}
