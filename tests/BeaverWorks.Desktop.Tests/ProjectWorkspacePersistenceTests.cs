using System;
using System.IO;
using BeaverWorks.Core.Models;
using BeaverWorks.Core.Persistence;
using BeaverWorks.Desktop.ViewModels;

namespace BeaverWorks.Desktop.Tests;

/// <summary>
/// Proves Risk #1 from context/foundation/test-plan.md: after a task is
/// pinned to the plan and saved, reopening the project (a genuinely new
/// <see cref="ProjectStore"/> load into a new <see cref="ProjectWorkspaceViewModel"/>,
/// not the original in-memory instance) reproduces the task with an
/// identical title and identical marker position. This exercises the
/// real production save trigger (<see cref="ProjectWorkspaceViewModel.AddTask"/>
/// -&gt; TrySave), which existing tests (ProjectStoreTests, PlanCoordinateMapperTests)
/// never touch.
/// </summary>
public class ProjectWorkspacePersistenceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _planImagePath;
    private readonly string _projectFilePath;

    public ProjectWorkspacePersistenceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"beaverworks-workspace-persistence-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);

        _planImagePath = Path.Combine(_tempDirectory, "plan.png");
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "Assets", "sample-floor-plan.png"),
            _planImagePath);

        _projectFilePath = Path.Combine(_tempDirectory, "project.bwproj");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
    
    [Fact]
    public void PinTaskThenReload_ReproducesTitleAndMarkerPositionExactly()
    {
        // Arrange: a brand-new project, saved via the real ViewModel save
        // trigger — no direct ProjectStore.Save call, matching how the app
        // actually creates/pins a task.
        var project = new Project
        {
            Name = "Kitchen remodel",
            PlanImagePath = _planImagePath,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var projectStore = new ProjectStore();
        var budgetProfileStore = new UserBudgetProfileStore(_tempDirectory);
        var workspace = new ProjectWorkspaceViewModel(
            project, _projectFilePath, projectStore, "test-user", budgetProfileStore);

        const string title = "Replace kitchen outlet";
        var position = new PlanPoint { X = 0.123456789, Y = 0.987654321 };
        var task = RenovationTask.Create(title, position);

        // Act: pin the task (this both mutates and persists via TrySave),
        // then simulate a genuine reopen — a fresh ProjectStore.Load call
        // into a second, independent ProjectWorkspaceViewModel, never
        // reusing the original `project`/`workspace` references.
        workspace.AddTask(task);

        var reloadedProject = new ProjectStore().Load(_projectFilePath);
        var reloadedWorkspace = new ProjectWorkspaceViewModel(
            reloadedProject, _projectFilePath, new ProjectStore(), "test-user", new UserBudgetProfileStore(_tempDirectory));

        // Assert: the reloaded task survives with an identical title and
        // marker position.
        var reloadedTask = Assert.Single(reloadedWorkspace.Project.Tasks);
        Assert.Equal(title, reloadedTask.Title);
        Assert.Equal(position.X, reloadedTask.Position.X, precision: 10);
        Assert.Equal(position.Y, reloadedTask.Position.Y, precision: 10);
    }
}
