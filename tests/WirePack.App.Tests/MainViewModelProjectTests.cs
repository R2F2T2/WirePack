using Avalonia.Media;
using WirePack.App.Models;
using WirePack.App.Project;
using WirePack.App.ViewModels;
using WirePack.Core.Geometry;
using WirePack.Core.Project;

namespace WirePack.App.Tests;

public class MainViewModelProjectTests
{
    [Fact]
    public async Task OpenProject_RestoresGroupsColorsAndVariant()
    {
        var file = new FakeProjectFileService
        {
            LoadResult = ProjectLoadResult.Success(CreateDocument())
        };
        var dialog = new FakeProjectDialogService { OpenPath = "/tmp/project.wpack" };
        var viewModel = new MainViewModel(file, dialog);

        await viewModel.OpenProjectCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsDirty);
        WireGroupModel group = Assert.Single(viewModel.WireGroups);
        Assert.Equal("group-1", group.Id);
        Assert.Equal(Color.FromArgb(255, 0x11, 0x22, 0x33), group.Color);
        Assert.Equal(2d, group.Diameter);
        Assert.Equal(2, group.Count);

        VariantDisplayModel variant = Assert.Single(viewModel.Variants);
        Assert.Same(variant, viewModel.SelectedVariant);
        Assert.Equal("variant-1", variant.Id);
        Assert.Equal(0.425d, variant.FillFactor);
        Assert.Equal(42.5d, variant.FillFactorPercent);
        Assert.Equal(new[] { "group-1", "group-1" }, variant.WireGroupIds);
        Assert.Equal(new Point(2d, 0d), variant.Layout[1].Center);
    }

    [Fact]
    public async Task OpenProject_FailedLoad_KeepsCurrentProject()
    {
        var file = new FakeProjectFileService
        {
            LoadResult = ProjectLoadResult.Failure("Файл повреждён.")
        };
        var dialog = new FakeProjectDialogService
        {
            OpenPath = "/tmp/bad.wpack",
            Choice = UnsavedChangesChoice.Discard
        };
        var viewModel = new MainViewModel(file, dialog);
        string originalId = viewModel.WireGroups[0].Id;
        viewModel.WireGroups[0].Diameter = 3d;
        Assert.True(viewModel.IsDirty);

        await viewModel.OpenProjectCommand.ExecuteAsync(null);

        WireGroupModel group = Assert.Single(viewModel.WireGroups);
        Assert.Equal(originalId, group.Id);
        Assert.Equal(3d, group.Diameter);
        Assert.Empty(viewModel.Variants);
        Assert.True(viewModel.IsDirty);
        Assert.Contains("повреждён", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task OpenProject_DirtyPromptCancelled_DoesNotLoad()
    {
        var file = new FakeProjectFileService
        {
            LoadResult = ProjectLoadResult.Success(CreateDocument())
        };
        var dialog = new FakeProjectDialogService
        {
            OpenPath = "/tmp/project.wpack",
            Choice = UnsavedChangesChoice.Cancel
        };
        var viewModel = new MainViewModel(file, dialog);
        viewModel.IsDirty = true;

        await viewModel.OpenProjectCommand.ExecuteAsync(null);

        Assert.Equal(0, file.LoadCount);
        Assert.Equal(1, dialog.PromptCount);
        Assert.True(viewModel.IsDirty);
        Assert.Single(viewModel.WireGroups);
    }

    [Fact]
    public async Task OpenProject_DirtyDiscard_LoadsProject()
    {
        var file = new FakeProjectFileService
        {
            LoadResult = ProjectLoadResult.Success(CreateDocument())
        };
        var dialog = new FakeProjectDialogService
        {
            OpenPath = "/tmp/project.wpack",
            Choice = UnsavedChangesChoice.Discard
        };
        var viewModel = new MainViewModel(file, dialog);
        viewModel.IsDirty = true;

        await viewModel.OpenProjectCommand.ExecuteAsync(null);

        Assert.Equal(1, file.LoadCount);
        Assert.False(viewModel.IsDirty);
        Assert.Equal("group-1", viewModel.WireGroups[0].Id);
    }

    [Fact]
    public async Task SaveProject_PicksPathWhenMissing_ClearsDirty()
    {
        var file = new FakeProjectFileService();
        var dialog = new FakeProjectDialogService
        {
            SavePath = Path.Combine(Path.GetTempPath(), "WirePackTestProject")
        };
        var viewModel = new MainViewModel(file, dialog);
        viewModel.IsDirty = true;

        await viewModel.SaveProjectCommand.ExecuteAsync(null);

        Assert.Equal(1, file.SaveCount);
        Assert.EndsWith(".wpack", file.SavedPath ?? string.Empty);
        Assert.NotNull(file.SavedDocument);
        Assert.False(viewModel.IsDirty);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task SaveProject_UsesKnownPathWithoutDialog()
    {
        var file = new FakeProjectFileService();
        var dialog = new FakeProjectDialogService
        {
            SavePath = Path.Combine(Path.GetTempPath(), "first.wpack")
        };
        var viewModel = new MainViewModel(file, dialog);

        await viewModel.SaveProjectCommand.ExecuteAsync(null);
        string firstPath = file.SavedPath ?? string.Empty;

        viewModel.WireGroups[0].Count = 4;
        dialog.SavePath = null;
        await viewModel.SaveProjectCommand.ExecuteAsync(null);

        Assert.Equal(2, file.SaveCount);
        Assert.Equal(firstPath, file.SavedPath);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task SaveProjectAs_AlwaysUsesDialogAndUpdatesPath()
    {
        var file = new FakeProjectFileService();
        var dialog = new FakeProjectDialogService
        {
            SavePath = Path.Combine(Path.GetTempPath(), "first.wpack")
        };
        var viewModel = new MainViewModel(file, dialog);
        await viewModel.SaveProjectCommand.ExecuteAsync(null);

        string secondPath = Path.Combine(Path.GetTempPath(), "second");
        dialog.SavePath = secondPath;
        await viewModel.SaveProjectAsCommand.ExecuteAsync(null);

        Assert.Equal(2, file.SaveCount);
        Assert.Equal(secondPath + ".wpack", file.SavedPath);
    }

    [Fact]
    public async Task SaveProject_Error_KeepsDirty()
    {
        var file = new FakeProjectFileService();
        var dialog = new FakeProjectDialogService
        {
            SavePath = Path.Combine(Path.GetTempPath(), "project.wpack")
        };
        var viewModel = new MainViewModel(file, dialog);

        await viewModel.SaveProjectCommand.ExecuteAsync(null);
        viewModel.WireGroups[0].Count = 4;
        file.SaveException = new ProjectFileException("Диск недоступен.");
        await viewModel.SaveProjectCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsDirty);
        Assert.Contains("Диск недоступен", viewModel.ErrorMessage);
    }

    [Fact]
    public void IsDirty_TracksGroupAndVariantChanges()
    {
        var viewModel = new MainViewModel(new FakeProjectFileService(), new FakeProjectDialogService());
        Assert.False(viewModel.IsDirty);

        viewModel.WireGroups[0].Diameter = 2.5d;
        Assert.True(viewModel.IsDirty);

        viewModel.IsDirty = false;
        viewModel.WireGroups[0].Count = 4;
        Assert.True(viewModel.IsDirty);

        viewModel.IsDirty = false;
        viewModel.WireGroups[0].Color = Color.FromRgb(1, 2, 3);
        Assert.True(viewModel.IsDirty);

        viewModel.IsDirty = false;
        viewModel.AddGroupCommand.Execute(null);
        Assert.True(viewModel.IsDirty);

        viewModel.IsDirty = false;
        viewModel.RemoveGroupCommand.Execute(viewModel.WireGroups[1]);
        Assert.True(viewModel.IsDirty);

        viewModel.IsDirty = false;
        var layout = Layout.FromWires(new Wire(new Point(0d, 0d), 2d));
        var variant = new VariantDisplayModel(
            1,
            layout,
            new Tube(new Point(0d, 0d), 1d),
            fillFactor: 0.5d);
        viewModel.Variants.Add(variant);
        Assert.True(viewModel.IsDirty);
    }

    [Fact]
    public async Task ConfirmCloseAsync_WhenClean_ReturnsTrue()
    {
        var viewModel = new MainViewModel(new FakeProjectFileService(), new FakeProjectDialogService());

        Assert.True(await viewModel.ConfirmCloseAsync());
    }

    [Fact]
    public async Task ConfirmCloseAsync_WhenDirtyAndCancelled_ReturnsFalse()
    {
        var dialog = new FakeProjectDialogService { Choice = UnsavedChangesChoice.Cancel };
        var viewModel = new MainViewModel(new FakeProjectFileService(), dialog);
        viewModel.IsDirty = true;

        Assert.False(await viewModel.ConfirmCloseAsync());
        Assert.Equal(1, dialog.PromptCount);
    }

    [Fact]
    public async Task ConfirmCloseAsync_WhenDirtyAndSaved_SavesAndReturnsTrue()
    {
        var file = new FakeProjectFileService();
        var dialog = new FakeProjectDialogService
        {
            Choice = UnsavedChangesChoice.Save,
            SavePath = Path.Combine(Path.GetTempPath(), "close.wpack")
        };
        var viewModel = new MainViewModel(file, dialog);
        viewModel.IsDirty = true;

        Assert.True(await viewModel.ConfirmCloseAsync());
        Assert.Equal(1, file.SaveCount);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task ConfirmCloseAsync_WhenSaveFails_ReturnsFalse()
    {
        var file = new FakeProjectFileService
        {
            SaveException = new ProjectFileException("Ошибка записи.")
        };
        var dialog = new FakeProjectDialogService
        {
            Choice = UnsavedChangesChoice.Save,
            SavePath = Path.Combine(Path.GetTempPath(), "close.wpack")
        };
        var viewModel = new MainViewModel(file, dialog);
        viewModel.IsDirty = true;

        Assert.False(await viewModel.ConfirmCloseAsync());
        Assert.True(viewModel.IsDirty);
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

    private sealed class FakeProjectFileService : IProjectFileService
    {
        public ProjectLoadResult LoadResult { get; set; } = ProjectLoadResult.Failure("Не настроено.");
        public Exception? SaveException { get; set; }
        public string? SavedPath { get; private set; }
        public ProjectDocument? SavedDocument { get; private set; }
        public int LoadCount { get; private set; }
        public int SaveCount { get; private set; }

        public Task<ProjectLoadResult> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            LoadCount++;
            return Task.FromResult(LoadResult);
        }

        public Task SaveAsync(string path, ProjectDocument document, CancellationToken cancellationToken = default)
        {
            SaveCount++;

            if (SaveException is not null)
                throw SaveException;

            SavedPath = path;
            SavedDocument = document;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProjectDialogService : IProjectDialogService
    {
        public string? OpenPath { get; set; }
        public string? SavePath { get; set; }
        public UnsavedChangesChoice Choice { get; set; } = UnsavedChangesChoice.Cancel;
        public int PromptCount { get; private set; }

        public Task<string?> PickOpenProjectPathAsync() => Task.FromResult(OpenPath);

        public Task<string?> PickSaveProjectPathAsync(string? currentPath) => Task.FromResult(SavePath);

        public Task<UnsavedChangesChoice> ConfirmUnsavedChangesAsync(string actionDescription)
        {
            PromptCount++;
            return Task.FromResult(Choice);
        }
    }
}
