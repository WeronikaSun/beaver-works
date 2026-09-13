using BeaverWorks.Core.Models;
using BeaverWorks.Core.Persistence;

namespace BeaverWorks.Core.Tests.Persistence;

public class ProjectStoreTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _projectFilePath;
    private readonly string _planImagePath;

    public ProjectStoreTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"beaverworks-project-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        _projectFilePath = Path.Combine(_tempDirectory, "project.bwproj");
        _planImagePath = Path.Combine(_tempDirectory, "plan.png");
        File.WriteAllBytes(_planImagePath, [0x89, 0x50, 0x4E, 0x47]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsProjectWithTasks_PositionPreservedExactly()
    {
        var store = new ProjectStore();
        var position = new PlanPoint { X = 0.123456789, Y = 0.987654321 };
        var task = RenovationTask.Create("Replace outlet", position);
        var project = new Project
        {
            Name = "Test project",
            PlanImagePath = _planImagePath,
            Tasks = [task],
            CreatedAt = DateTimeOffset.UtcNow,
        };

        store.Save(project, _projectFilePath);
        var loaded = store.Load(_projectFilePath);

        var loadedTask = Assert.Single(loaded.Tasks);
        Assert.Equal(task.Title, loadedTask.Title);
        Assert.Equal(position.X, loadedTask.Position.X);
        Assert.Equal(position.Y, loadedTask.Position.Y);
        Assert.Equal(RenovationTaskStatus.Planned, loadedTask.Status);
    }

    [Fact]
    public void Load_MissingPlanImage_ThrowsPlanImageMissingExceptionAndLeavesProjectFileUntouched()
    {
        var store = new ProjectStore();
        var project = new Project
        {
            Name = "Test project",
            PlanImagePath = _planImagePath,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        store.Save(project, _projectFilePath);

        File.Delete(_planImagePath);
        var originalJson = File.ReadAllText(_projectFilePath);

        var exception = Assert.Throws<PlanImageMissingException>(() => store.Load(_projectFilePath));

        Assert.Equal(_planImagePath, exception.PlanImagePath);
        Assert.Equal(originalJson, File.ReadAllText(_projectFilePath));
    }
}
