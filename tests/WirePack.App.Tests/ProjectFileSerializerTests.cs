using WirePack.App.Project;
using WirePack.Core.Project;

namespace WirePack.App.Tests;

public class ProjectFileSerializerTests
{
    private const string GroupId = "group-1";

    [Fact]
    public void SerializeAndDeserialize_ValidProject_RoundTripsData()
    {
        ProjectDocument document = CreateValidDocument();

        string json = ProjectFileSerializer.Serialize(document);
        ProjectLoadResult result = ProjectFileSerializer.Deserialize(json);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Issues);

        ProjectDocument loaded = Assert.IsType<ProjectDocument>(result.Document);
        Assert.Equal(ProjectFormat.CurrentVersion, loaded.SchemaVersion);
        Assert.Single(loaded.WireGroups);
        Assert.Equal(GroupId, loaded.WireGroups[0].Id);
        Assert.Equal("#FF3B82F6", loaded.WireGroups[0].Color);
        Assert.Equal(2d, loaded.WireGroups[0].Diameter);
        Assert.Equal(2, loaded.WireGroups[0].Count);
        Assert.Single(loaded.Variants);

        LayoutVariantDto variant = loaded.Variants[0];
        Assert.Equal("variant-1", variant.Id);
        Assert.Equal(4d, variant.TubeDiameter);
        Assert.Equal(1d, variant.TubeCenter.X);
        Assert.Equal(0d, variant.TubeCenter.Y);
        Assert.Equal(0.425d, variant.FillFactor);
        Assert.Equal(2, variant.Wires.Count);
        Assert.All(variant.Wires, wire => Assert.Equal(GroupId, wire.GroupId));
        Assert.Equal(new ProjectPoint(2d, 0d), variant.Wires[1].Center);
    }

    [Fact]
    public void Serialize_UsesCamelCasePropertyNames()
    {
        string json = ProjectFileSerializer.Serialize(CreateValidDocument());

        Assert.Contains("\"schemaVersion\"", json);
        Assert.Contains("\"wireGroups\"", json);
        Assert.Contains("\"variants\"", json);
        Assert.Contains("\"color\"", json);
        Assert.Contains("\"groupId\"", json);
        Assert.Contains("\"fillFactor\"", json);
        Assert.Contains("\"tubeDiameter\"", json);
        Assert.Contains("\"tubeCenter\"", json);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("{")]
    [InlineData("null")]
    public void Deserialize_EmptyOrMalformedJson_ReturnsFailure(string? json)
    {
        ProjectLoadResult result = ProjectFileSerializer.Deserialize(json);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Document);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public void Deserialize_MissingRequiredField_ReturnsFailure()
    {
        const string json = """
            {
              "wireGroups": [
                { "id": "group-1", "diameter": 2.0, "count": 2, "color": "#FF3B82F6" }
              ],
              "variants": []
            }
            """;

        ProjectLoadResult result = ProjectFileSerializer.Deserialize(json);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("повреждён", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_MissingNestedRequiredField_ReturnsFailure()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "wireGroups": [
                { "id": "group-1", "diameter": 2.0, "count": 1, "color": "#FF3B82F6" }
              ],
              "variants": [
                {
                  "id": "variant-1",
                  "tubeDiameter": 2.0,
                  "fillFactor": 0.5,
                  "wires": [
                    { "groupId": "group-1", "diameter": 2.0, "center": { "x": 0.0, "y": 0.0 } }
                  ]
                }
              ]
            }
            """;

        ProjectLoadResult result = ProjectFileSerializer.Deserialize(json);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("повреждён", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_UnsupportedSchemaVersion_ReturnsFailure()
    {
        ProjectDocument document = CreateValidDocument();
        document = new ProjectDocument
        {
            SchemaVersion = ProjectFormat.CurrentVersion + 1,
            WireGroups = document.WireGroups,
            Variants = document.Variants
        };

        ProjectLoadResult result = ProjectFileSerializer.Deserialize(
            ProjectFileSerializer.Serialize(document));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.UnsupportedFormatVersion);
    }

    [Fact]
    public void Deserialize_OutdatedResults_RemovesVariants()
    {
        var document = new ProjectDocument
        {
            SchemaVersion = ProjectFormat.CurrentVersion,
            WireGroups = [new WireGroupDto { Id = GroupId, Diameter = 2d, Count = 2, Color = "#FF3B82F6" }],
            Variants =
            [
                new LayoutVariantDto
                {
                    Id = "outdated",
                    TubeDiameter = 5.5d,
                    TubeCenter = new ProjectPoint(1.25d, 0d),
                    FillFactor = 0.5d,
                    Wires =
                    [
                        new ProjectWireDto { GroupId = GroupId, Diameter = 3d, Center = new ProjectPoint(0d, 0d) },
                        new ProjectWireDto { GroupId = GroupId, Diameter = 2d, Center = new ProjectPoint(3d, 0d) }
                    ]
                }
            ]
        };

        ProjectLoadResult result = ProjectFileSerializer.Deserialize(
            ProjectFileSerializer.Serialize(document));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Document);
        Assert.Empty(result.Document.Variants);
        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.OutdatedResults);
    }

    [Fact]
    public void Deserialize_InvalidVariant_RemovesOnlyInvalidVariant()
    {
        ProjectDocument valid = CreateValidDocument();
        var document = new ProjectDocument
        {
            SchemaVersion = valid.SchemaVersion,
            WireGroups = valid.WireGroups,
            Variants =
            [
                CreateValidVariant("valid"),
                CreateValidVariant("invalid", tubeCenter: new ProjectPoint(2d, 0d))
            ]
        };

        ProjectLoadResult result = ProjectFileSerializer.Deserialize(
            ProjectFileSerializer.Serialize(document));

        Assert.True(result.IsSuccess);
        ProjectDocument loaded = Assert.IsType<ProjectDocument>(result.Document);
        LayoutVariantDto remaining = Assert.Single(loaded.Variants);
        Assert.Equal("valid", remaining.Id);
        Assert.Contains(result.Issues, issue =>
            issue.Kind == ProjectValidationIssueKind.MecCenterMismatch &&
            issue.VariantId == "invalid");
    }

    private static ProjectDocument CreateValidDocument() => new()
    {
        SchemaVersion = ProjectFormat.CurrentVersion,
        WireGroups =
        [
            new WireGroupDto
            {
                Id = GroupId,
                Diameter = 2d,
                Count = 2,
                Color = "#FF3B82F6"
            }
        ],
        Variants = [CreateValidVariant("variant-1")]
    };

    private static LayoutVariantDto CreateValidVariant(
        string id,
        ProjectPoint? tubeCenter = null) => new()
        {
            Id = id,
            TubeDiameter = 4d,
            TubeCenter = tubeCenter ?? new ProjectPoint(1d, 0d),
            FillFactor = 0.425d,
            Wires =
            [
                new ProjectWireDto { GroupId = GroupId, Diameter = 2d, Center = new ProjectPoint(0d, 0d) },
                new ProjectWireDto { GroupId = GroupId, Diameter = 2d, Center = new ProjectPoint(2d, 0d) }
            ]
        };
}
