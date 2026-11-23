namespace WirePack.Core.Tests;

public class ProjectDocumentValidationTests
{
    private const string GroupId = "group-1";
    private const string GroupColor = "#FF3B82F6";

    [Fact]
    public void Validate_ValidProject_ReturnsSuccess()
    {
        ProjectDocument document = CreateDocument(CreateValidVariant("variant-1"));

        ProjectValidationResult result = ProjectDocumentValidator.Validate(document);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_UnsupportedSchemaVersion_ReturnsError()
    {
        var document = new ProjectDocument
        {
            SchemaVersion = ProjectFormat.CurrentVersion + 1,
            WireGroups = CreateGroups(),
            Variants = []
        };

        ProjectValidationResult result = ProjectDocumentValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.UnsupportedFormatVersion);
    }

    [Fact]
    public void Validate_NoWireGroups_ReturnsFatalError()
    {
        var document = new ProjectDocument
        {
            WireGroups = null!,
            Variants = []
        };

        ProjectValidationResult result = ProjectDocumentValidator.Validate(document);

        Assert.True(result.HasFatalErrors);
        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.MissingWireGroups);
    }

    [Fact]
    public void Validate_EmptyWireGroups_ReturnsSuccess()
    {
        var document = new ProjectDocument
        {
            WireGroups = [],
            Variants = []
        };

        ProjectValidationResult result = ProjectDocumentValidator.Validate(document);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_InvalidGroupData_ReturnsErrors()
    {
        var document = new ProjectDocument
        {
            WireGroups =
            [
                new WireGroupDto { Id = "g1", Diameter = 0d, Count = 0, Color = GroupColor },
                new WireGroupDto { Id = "g2", Diameter = double.NaN, Count = -1, Color = GroupColor }
            ],
            Variants = []
        };

        ProjectValidationResult result = ProjectDocumentValidator.Validate(document);

        Assert.True(result.HasFatalErrors);
        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.InvalidWireGroupDiameter);
        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.InvalidWireGroupCount);
    }

    [Fact]
    public void Validate_DuplicateGroupIds_ReturnsError()
    {
        var document = new ProjectDocument
        {
            WireGroups =
            [
                new WireGroupDto { Id = "same", Diameter = 2d, Count = 1, Color = GroupColor },
                new WireGroupDto { Id = "same", Diameter = 3d, Count = 1, Color = GroupColor }
            ],
            Variants = []
        };

        ProjectValidationResult result = ProjectDocumentValidator.Validate(document);

        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.DuplicateWireGroupId);
    }

    [Fact]
    public void Validate_InvalidGroupColor_ReturnsError()
    {
        var document = new ProjectDocument
        {
            WireGroups =
            [
                new WireGroupDto { Id = "g1", Diameter = 2d, Count = 1, Color = "red" }
            ],
            Variants = []
        };

        ProjectValidationResult result = ProjectDocumentValidator.Validate(document);

        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.InvalidWireGroupColor);
    }

    [Fact]
    public void Validate_WireCountExceeded_ReturnsError()
    {
        var document = new ProjectDocument
        {
            WireGroups =
            [
                new WireGroupDto
                {
                    Id = "g1",
                    Diameter = 1d,
                    Count = ProjectFormat.MaxWireCount + 1,
                    Color = GroupColor
                }
            ],
            Variants = []
        };

        ProjectValidationResult result = ProjectDocumentValidator.Validate(document);

        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.WireCountExceeded);
    }

    [Fact]
    public void Validate_DuplicateVariantIds_ReturnsErrors()
    {
        ProjectDocument document = CreateDocument(
            CreateValidVariant("same-id"),
            CreateValidVariant("same-id"));

        ProjectValidationResult result = ProjectDocumentValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.DuplicateVariantId &&
            issue.VariantIndex == 0);
        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.DuplicateVariantId &&
            issue.VariantIndex == 1);
    }

    [Fact]
    public void Validate_MissingVariantId_ReturnsError()
    {
        ProjectDocument document = CreateDocument(CreateValidVariant(string.Empty));

        ProjectValidationResult result = ProjectDocumentValidator.Validate(document);

        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.MissingVariantId);
    }

    [Fact]
    public void Validate_InvalidFillFactor_ReturnsError()
    {
        LayoutVariantDto variant = CreateValidVariant("variant-1");
        variant = new LayoutVariantDto
        {
            Id = variant.Id,
            TubeDiameter = variant.TubeDiameter,
            TubeCenter = variant.TubeCenter,
            FillFactor = double.NaN,
            Wires = variant.Wires
        };

        ProjectValidationResult result = ProjectDocumentValidator.Validate(CreateDocument(variant));

        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.InvalidFillFactor);
    }

    [Fact]
    public void Validate_MecCenterMismatch_ReturnsError()
    {
        LayoutVariantDto variant = CreateValidVariant(
            "variant-1",
            tubeCenter: new ProjectPoint(1.5d, 0d));

        ProjectValidationResult result = ProjectDocumentValidator.Validate(CreateDocument(variant));

        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.MecCenterMismatch);
    }

    [Fact]
    public void Validate_MecDiameterMismatch_ReturnsError()
    {
        LayoutVariantDto variant = CreateValidVariant(
            "variant-1",
            tubeDiameter: 4.5d);

        ProjectValidationResult result = ProjectDocumentValidator.Validate(CreateDocument(variant));

        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.MecDiameterMismatch);
    }

    [Fact]
    public void Validate_InvalidCoordinates_ReturnsErrorWithoutException()
    {
        var variant = new LayoutVariantDto
        {
            Id = "variant-1",
            TubeDiameter = 4d,
            TubeCenter = new ProjectPoint(1d, 0d),
            FillFactor = 0.5d,
            Wires =
            [
                new ProjectWireDto { GroupId = GroupId, Diameter = 2d, Center = new ProjectPoint(double.NaN, 0d) },
                new ProjectWireDto { GroupId = GroupId, Diameter = 2d, Center = new ProjectPoint(2d, 0d) }
            ]
        };

        ProjectValidationResult result = ProjectDocumentValidator.Validate(CreateDocument(variant));

        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.InvalidCoordinates);
    }

    [Fact]
    public void Validate_IntersectingWires_ReturnsGeometryError()
    {
        var variant = new LayoutVariantDto
        {
            Id = "variant-1",
            TubeDiameter = 5d,
            TubeCenter = new ProjectPoint(0.5d, 0d),
            FillFactor = 0.5d,
            Wires =
            [
                new ProjectWireDto { GroupId = GroupId, Diameter = 4d, Center = new ProjectPoint(0d, 0d) },
                new ProjectWireDto { GroupId = GroupId, Diameter = 4d, Center = new ProjectPoint(1d, 0d) }
            ]
        };

        ProjectValidationResult result = ProjectDocumentValidator.Validate(CreateDocument(variant));

        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.InvalidVariantGeometry);
    }

    [Fact]
    public void Validate_WireSetMismatch_MarksResultsAsOutdated()
    {
        var variant = new LayoutVariantDto
        {
            Id = "variant-1",
            TubeDiameter = 5.5d,
            TubeCenter = new ProjectPoint(1.25d, 0d),
            FillFactor = 0.5d,
            Wires =
            [
                new ProjectWireDto { GroupId = GroupId, Diameter = 3d, Center = new ProjectPoint(0d, 0d) },
                new ProjectWireDto { GroupId = GroupId, Diameter = 2d, Center = new ProjectPoint(3d, 0d) }
            ]
        };

        ProjectValidationResult result = ProjectDocumentValidator.Validate(CreateDocument(variant));

        Assert.True(result.IsValid);
        Assert.True(result.HasOutdatedResults);
        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.OutdatedResults &&
            issue.Severity == ProjectValidationSeverity.Warning);
    }

    [Fact]
    public void Validate_MissingGroupReference_ReturnsError()
    {
        var variant = new LayoutVariantDto
        {
            Id = "variant-1",
            TubeDiameter = 4d,
            TubeCenter = new ProjectPoint(1d, 0d),
            FillFactor = 0.5d,
            Wires =
            [
                new ProjectWireDto { GroupId = "unknown", Diameter = 2d, Center = new ProjectPoint(0d, 0d) },
                new ProjectWireDto { GroupId = GroupId, Diameter = 2d, Center = new ProjectPoint(2d, 0d) }
            ]
        };

        ProjectValidationResult result = ProjectDocumentValidator.Validate(CreateDocument(variant));

        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.MissingWireGroupReference);
    }

    private static ProjectDocument CreateDocument(params LayoutVariantDto[] variants) => new()
    {
        SchemaVersion = ProjectFormat.CurrentVersion,
        WireGroups = CreateGroups(),
        Variants = variants.ToList()
    };

    private static List<WireGroupDto> CreateGroups() =>
        [new WireGroupDto { Id = GroupId, Diameter = 2d, Count = 2, Color = GroupColor }];

    private static LayoutVariantDto CreateValidVariant(
        string id,
        double tubeDiameter = 4d,
        ProjectPoint? tubeCenter = null) => new()
        {
            Id = id,
            TubeDiameter = tubeDiameter,
            TubeCenter = tubeCenter ?? new ProjectPoint(1d, 0d),
            FillFactor = 0.5d,
            Wires =
            [
                new ProjectWireDto { GroupId = GroupId, Diameter = 2d, Center = new ProjectPoint(0d, 0d) },
                new ProjectWireDto { GroupId = GroupId, Diameter = 2d, Center = new ProjectPoint(2d, 0d) }
            ]
        };
}
