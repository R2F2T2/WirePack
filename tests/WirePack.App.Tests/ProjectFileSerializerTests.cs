using WirePack.App.Project;
using WirePack.Core.Project;

namespace WirePack.App.Tests;

public class ProjectFileSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_ValidProject_RoundTripsData()
    {
        ProjectDocument document = CreateValidDocument();

        string json = ProjectFileSerializer.Serialize(document);
        ProjectLoadResult result = ProjectFileSerializer.Deserialize(json);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Issues);

        ProjectDocument loaded = Assert.IsType<ProjectDocument>(result.Document);
        Assert.Equal(ProjectFormat.CurrentVersion, loaded.FormatVersion);
        Assert.Single(loaded.WireGroups);
        Assert.Equal(2d, loaded.WireGroups[0].Diameter);
        Assert.Equal(2, loaded.WireGroups[0].Count);
        Assert.Single(loaded.Variants);

        LayoutVariantDto variant = loaded.Variants[0];
        Assert.Equal("variant-1", variant.Id);
        Assert.Equal(4d, variant.TubeDiameter);
        Assert.Equal(1d, variant.TubeCenter.X);
        Assert.Equal(0d, variant.TubeCenter.Y);
        Assert.Equal(2, variant.Wires.Count);
        Assert.Equal(new ProjectPoint(2d, 0d), variant.Wires[1].Center);
    }

    [Fact]
    public void Serialize_UsesCamelCasePropertyNames()
    {
        string json = ProjectFileSerializer.Serialize(CreateValidDocument());

        Assert.Contains("\"formatVersion\"", json);
        Assert.Contains("\"wireGroups\"", json);
        Assert.Contains("\"variants\"", json);
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
                { "diameter": 2.0, "count": 2 }
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
              "formatVersion": 1,
              "wireGroups": [
                { "diameter": 2.0, "count": 1 }
              ],
              "variants": [
                {
                  "id": "variant-1",
                  "tubeDiameter": 2.0,
                  "wires": [
                    { "diameter": 2.0, "center": { "x": 0.0, "y": 0.0 } }
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
    public void Deserialize_MissingRequiredPointCoordinate_ReturnsFailure()
    {
        const string json = """
            {
              "formatVersion": 1,
              "wireGroups": [
                { "diameter": 2.0, "count": 1 }
              ],
              "variants": [
                {
                  "id": "variant-1",
                  "tubeDiameter": 2.0,
                  "tubeCenter": { "x": 0.0 },
                  "wires": [
                    { "diameter": 2.0, "center": { "x": 0.0, "y": 0.0 } }
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
    public void Deserialize_UnsupportedVersion_ReturnsFailure()
    {
        ProjectDocument document = CreateValidDocument();
        document = new ProjectDocument
        {
            FormatVersion = ProjectFormat.CurrentVersion + 1,
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
            FormatVersion = ProjectFormat.CurrentVersion,
            WireGroups = [new WireGroupDto { Diameter = 2d, Count = 2 }],
            Variants =
            [
                new LayoutVariantDto
                {
                    Id = "outdated",
                    TubeDiameter = 5.5d,
                    TubeCenter = new ProjectPoint(1.25d, 0d),
                    Wires =
                    [
                        new ProjectWireDto { Diameter = 3d, Center = new ProjectPoint(0d, 0d) },
                        new ProjectWireDto { Diameter = 2d, Center = new ProjectPoint(3d, 0d) }
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
            FormatVersion = valid.FormatVersion,
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

    [Fact]
    public void SaveAndLoad_File_RoundTripsProject()
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("WirePackProjectTests");
        string path = Path.Combine(directory.FullName, "project.json");

        try
        {
            ProjectDocument document = CreateValidDocument();

            ProjectFileSerializer.Save(path, document);
            ProjectLoadResult result = ProjectFileSerializer.Load(path);

            Assert.True(result.IsSuccess);
            ProjectDocument loaded = Assert.IsType<ProjectDocument>(result.Document);
            Assert.Equal(document.FormatVersion, loaded.FormatVersion);
            Assert.Equal(document.WireGroups.Count, loaded.WireGroups.Count);
            Assert.Equal(document.Variants.Count, loaded.Variants.Count);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsFailure()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        ProjectLoadResult result = ProjectFileSerializer.Load(path);

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    private static ProjectDocument CreateValidDocument() => new()
    {
        FormatVersion = ProjectFormat.CurrentVersion,
        WireGroups = [new WireGroupDto { Diameter = 2d, Count = 2 }],
        Variants = [CreateValidVariant("variant-1")]
    };

    private static LayoutVariantDto CreateValidVariant(
        string id,
        ProjectPoint? tubeCenter = null) => new()
        {
            Id = id,
            TubeDiameter = 4d,
            TubeCenter = tubeCenter ?? new ProjectPoint(1d, 0d),
            Wires =
            [
                new ProjectWireDto { Diameter = 2d, Center = new ProjectPoint(0d, 0d) },
                new ProjectWireDto { Diameter = 2d, Center = new ProjectPoint(2d, 0d) }
            ]
        };
}
