using Avalonia.Media;
using WirePack.App.Models;
using WirePack.App.Project;
using WirePack.Core.Geometry;
using WirePack.Core.Project;

namespace WirePack.App.Tests;

public class ProjectDocumentMapperTests
{
    [Fact]
    public void CreateDocument_MapsGroupsVariantsColorsAndFillFactor()
    {
        var group = new WireGroupModel
        {
            Id = "group-1",
            Diameter = 2d,
            Count = 2,
            Color = Color.FromArgb(255, 0x11, 0x22, 0x33)
        };
        var layout = Layout.FromWires(
            new Wire(new Point(0d, 0d), 2d),
            new Wire(new Point(2d, 0d), 2d));
        var tube = new Tube(new Point(1d, 0d), 2d);
        var variant = new VariantDisplayModel(
            1,
            layout,
            tube,
            wireGroupIds: new[] { "group-1", "group-1" },
            fillFactor: 0.425d,
            id: "variant-1");

        ProjectDocument document = ProjectDocumentMapper.CreateDocument(
            new[] { group },
            new[] { variant });

        Assert.Equal(ProjectFormat.CurrentVersion, document.SchemaVersion);
        Assert.Equal("group-1", document.WireGroups[0].Id);
        Assert.Equal("#FF112233", document.WireGroups[0].Color);
        Assert.Equal("variant-1", document.Variants[0].Id);
        Assert.Equal(0.425d, document.Variants[0].FillFactor);
        Assert.All(document.Variants[0].Wires, wire => Assert.Equal("group-1", wire.GroupId));
        Assert.Equal(new ProjectPoint(2d, 0d), document.Variants[0].Wires[1].Center);
    }

    [Fact]
    public void CreateWireGroups_RestoresIdsAndColors()
    {
        ProjectDocument document = CreateDocument();

        List<WireGroupModel> groups = ProjectDocumentMapper.CreateWireGroups(document);

        WireGroupModel group = Assert.Single(groups);
        Assert.Equal("group-1", group.Id);
        Assert.Equal(2d, group.Diameter);
        Assert.Equal(2, group.Count);
        Assert.Equal(Color.FromArgb(255, 0x11, 0x22, 0x33), group.Color);
    }

    [Fact]
    public void CreateVariants_RestoresFillFactorCoordinatesAndMembership()
    {
        ProjectDocument document = CreateDocument();
        List<WireGroupModel> groups = ProjectDocumentMapper.CreateWireGroups(document);

        List<VariantDisplayModel> variants = ProjectDocumentMapper.CreateVariants(document, groups);

        VariantDisplayModel variant = Assert.Single(variants);
        Assert.Equal("variant-1", variant.Id);
        Assert.Equal(0.425d, variant.FillFactor);
        Assert.Equal(42.5d, variant.FillFactorPercent);
        Assert.Equal(new[] { "group-1", "group-1" }, variant.WireGroupIds);
        Assert.Equal(Color.FromArgb(255, 0x11, 0x22, 0x33), variant.WireColors![0]);
        Assert.Equal(new Point(2d, 0d), variant.Layout[1].Center);
    }

    [Fact]
    public void ProjectColorConverter_RoundTripsArgbColor()
    {
        Color color = Color.FromArgb(0x80, 0x11, 0x22, 0x33);

        string hex = ProjectColorConverter.ToHex(color);
        Color parsed = ProjectColorConverter.FromHex(hex, Colors.Black);

        Assert.Equal("#80112233", hex);
        Assert.Equal(color, parsed);
    }

    private static ProjectDocument CreateDocument() => new()
    {
        SchemaVersion = ProjectFormat.CurrentVersion,
        WireGroups =
        [
            new WireGroupDto
            {
                Id = "group-1",
                Diameter = 2d,
                Count = 2,
                Color = "#FF112233"
            }
        ],
        Variants =
        [
            new LayoutVariantDto
            {
                Id = "variant-1",
                TubeDiameter = 4d,
                TubeCenter = new ProjectPoint(1d, 0d),
                FillFactor = 0.425d,
                Wires =
                [
                    new ProjectWireDto
                    {
                        GroupId = "group-1",
                        Diameter = 2d,
                        Center = new ProjectPoint(0d, 0d)
                    },
                    new ProjectWireDto
                    {
                        GroupId = "group-1",
                        Diameter = 2d,
                        Center = new ProjectPoint(2d, 0d)
                    }
                ]
            }
        ]
    };
}
