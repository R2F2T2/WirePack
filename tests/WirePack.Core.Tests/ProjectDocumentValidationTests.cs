namespace WirePack.Core.Tests;

public class ProjectDocumentValidationTests
{
    [Fact]
    public void Validate_ValidProject_ReturnsSuccess()
    {
        ProjectDocument document = CreateDocument(CreateValidVariant("variant-1"));

        ProjectValidationResult result = ProjectDocumentValidator.Validate(document);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_UnsupportedFormatVersion_ReturnsError()
    {
        ProjectDocument document = new()
        {
            FormatVersion = ProjectFormat.CurrentVersion + 1,
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
                new WireGroupDto { Diameter = 0d, Count = 0 },
                new WireGroupDto { Diameter = double.NaN, Count = -1 }
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
    public void Validate_WireCountExceeded_ReturnsError()
    {
        var document = new ProjectDocument
        {
            WireGroups = [new WireGroupDto { Diameter = 1d, Count = ProjectFormat.MaxWireCount + 1 }],
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
            Wires =
            [
                new ProjectWireDto { Diameter = 2d, Center = new ProjectPoint(double.NaN, 0d) },
                new ProjectWireDto { Diameter = 2d, Center = new ProjectPoint(2d, 0d) }
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
            Wires =
            [
                new ProjectWireDto { Diameter = 4d, Center = new ProjectPoint(0d, 0d) },
                new ProjectWireDto { Diameter = 4d, Center = new ProjectPoint(1d, 0d) }
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
            Wires =
            [
                new ProjectWireDto { Diameter = 3d, Center = new ProjectPoint(0d, 0d) },
                new ProjectWireDto { Diameter = 2d, Center = new ProjectPoint(3d, 0d) }
            ]
        };

        ProjectValidationResult result = ProjectDocumentValidator.Validate(CreateDocument(variant));

        Assert.True(result.IsValid);
        Assert.True(result.HasOutdatedResults);
        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.OutdatedResults &&
            issue.Severity == ProjectValidationSeverity.Warning);
    }

    private static ProjectDocument CreateDocument(params LayoutVariantDto[] variants) => new()
    {
        FormatVersion = ProjectFormat.CurrentVersion,
        WireGroups = CreateGroups(),
        Variants = variants.ToList()
    };

    private static List<WireGroupDto> CreateGroups() =>
        [new WireGroupDto { Diameter = 2d, Count = 2 }];

    private static LayoutVariantDto CreateValidVariant(
        string id,
        double tubeDiameter = 4d,
        ProjectPoint? tubeCenter = null) => new()
        {
            Id = id,
            TubeDiameter = tubeDiameter,
            TubeCenter = tubeCenter ?? new ProjectPoint(1d, 0d),
            Wires =
            [
                new ProjectWireDto { Diameter = 2d, Center = new ProjectPoint(0d, 0d) },
                new ProjectWireDto { Diameter = 2d, Center = new ProjectPoint(2d, 0d) }
            ]
        };
}
