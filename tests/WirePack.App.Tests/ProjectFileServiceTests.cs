using WirePack.App.Project;
using WirePack.Core.Project;

namespace WirePack.App.Tests;

public class ProjectFileServiceTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsProject()
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("WirePackProjectServiceTests");
        string path = Path.Combine(directory.FullName, "project.wpack");

        try
        {
            var service = new ProjectFileService();
            ProjectDocument document = CreateDocument();

            await service.SaveAsync(path, document);
            ProjectLoadResult result = await service.LoadAsync(path);

            Assert.True(result.IsSuccess);
            ProjectDocument loaded = Assert.IsType<ProjectDocument>(result.Document);
            Assert.Equal(document.SchemaVersion, loaded.SchemaVersion);
            Assert.Equal(document.WireGroups.Count, loaded.WireGroups.Count);
            Assert.Equal(document.Variants.Count, loaded.Variants.Count);
            Assert.Equal(0.425d, loaded.Variants[0].FillFactor);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Save_InvalidPath_ThrowsProjectFileException()
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("WirePackProjectServiceTests");
        string path = Path.Combine(directory.FullName, "missing", "project.wpack");

        try
        {
            var service = new ProjectFileService();

            await Assert.ThrowsAsync<ProjectFileException>(
                () => service.SaveAsync(path, CreateDocument()));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Load_MissingFile_ReturnsFailure()
    {
        var service = new ProjectFileService();
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        ProjectLoadResult result = await service.LoadAsync(path);

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public async Task Load_CorruptFile_ReturnsFailure()
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("WirePackProjectServiceTests");
        string path = Path.Combine(directory.FullName, "project.wpack");

        try
        {
            await File.WriteAllTextAsync(path, "{ неверный json");
            var service = new ProjectFileService();

            ProjectLoadResult result = await service.LoadAsync(path);

            Assert.False(result.IsSuccess);
            Assert.Null(result.Document);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
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
                Color = "#FF3B82F6"
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
