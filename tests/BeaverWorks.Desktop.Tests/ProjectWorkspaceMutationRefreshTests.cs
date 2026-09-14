using System;
using System.IO;
using BeaverWorks.Core.Models;
using BeaverWorks.Core.Persistence;
using BeaverWorks.Desktop.ViewModels;

namespace BeaverWorks.Desktop.Tests;

/// <summary>
/// Proves Risk #2 from context/foundation/test-plan.md: a task mutation
/// driven through <see cref="ProjectWorkspaceViewModel.UpdateTask(RenovationTask)"/>
/// or <see cref="ProjectWorkspaceViewModel.DeleteTask"/> is (a) persisted via
/// the real save path and (b) reflected in both dependent panel view
/// models (<see cref="ProjectWorkspaceViewModel.TaskList"/> and
/// <see cref="ProjectWorkspaceViewModel.Canvas"/>) — not just in the
/// underlying <see cref="Project.Tasks"/> model. Neither panel observes
/// <c>Project.Tasks</c> live; each is refreshed only via an explicit call
/// made by the parent after a successful save, so a test that checked only
/// <c>Project.Tasks</c> would still pass if that refresh call were
/// accidentally removed. Both an immediate, same-session check and a
/// genuine reload (fresh <see cref="ProjectStore.Load"/> into a second,
/// independent <see cref="ProjectWorkspaceViewModel"/>) are asserted.
/// </summary>
public class ProjectWorkspaceMutationRefreshTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _planImagePath;
    private readonly string _projectFilePath;

    public ProjectWorkspaceMutationRefreshTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"beaverworks-workspace-mutation-refresh-tests-{Guid.NewGuid()}");
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MutateTaskThenReload_PersistsAndRefreshesBothPanels(bool isEdit)
    {
        // Arrange: seed a project with one existing task, saved up front
        // (via a direct ProjectStore.Save, not through a ViewModel — this
        // test's subject is the edit/delete path, not task creation) so
        // both the edit and delete cases start from a task that already
        // exists on disk and in both panels.
        var project = new Project
        {
            Name = "Kitchen remodel",
            PlanImagePath = _planImagePath,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var seedTask = RenovationTask.Create("Replace kitchen outlet", new PlanPoint { X = 0.25, Y = 0.5 });
        project.Tasks.Add(seedTask);

        var projectStore = new ProjectStore();
        projectStore.Save(project, _projectFilePath);

        var budgetProfileStore = new UserBudgetProfileStore(_tempDirectory);
        var workspace = new ProjectWorkspaceViewModel(
            project, _projectFilePath, projectStore, "test-user", budgetProfileStore);

        // Act: mutate via the real production entry point behind the
        // Edit/Delete commands (ProjectWorkspaceViewModel.UpdateTask /
        // DeleteTask), not via direct collection manipulation.
        const string updatedTitle = "Replace kitchen outlet (GFCI)";
        if (isEdit)
        {
            var updatedTask = new RenovationTask
            {
                Id = seedTask.Id,
                Title = updatedTitle,
                Position = seedTask.Position,
                CreatedAt = seedTask.CreatedAt,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            workspace.UpdateTask(updatedTask);
        }
        else
        {
            workspace.DeleteTask(seedTask.Id);
        }

        // Assert (same session, before reload): the mutation must be
        // visible in all three surfaces — the model, the task list panel,
        // and the canvas markers panel — not just the model.
        AssertMutationReflected(workspace, seedTask.Id, updatedTitle, isEdit);

        // Reload: a genuinely new ProjectStore.Load call into a second,
        // independent ProjectWorkspaceViewModel — never reusing the
        // original project/workspace references, matching the Phase 1
        // reload convention.
        var reloadedProject = new ProjectStore().Load(_projectFilePath);
        var reloadedWorkspace = new ProjectWorkspaceViewModel(
            reloadedProject, _projectFilePath, new ProjectStore(), "test-user", new UserBudgetProfileStore(_tempDirectory));

        // Assert (after reload): the mutation survived the round trip and
        // is reflected in both panels of the freshly-constructed workspace.
        AssertMutationReflected(reloadedWorkspace, seedTask.Id, updatedTitle, isEdit);
    }

    private static void AssertMutationReflected(
        ProjectWorkspaceViewModel workspace, Guid taskId, string updatedTitle, bool isEdit)
    {
        if (isEdit)
        {
            Assert.Contains(workspace.Project.Tasks, t => t.Id == taskId && t.Title == updatedTitle);
            Assert.Contains(workspace.TaskList.Tasks, t => t.Id == taskId && t.Title == updatedTitle);
            Assert.Contains(workspace.Canvas.Markers, m => m.Task.Id == taskId && m.Task.Title == updatedTitle);
        }
        else
        {
            Assert.DoesNotContain(workspace.Project.Tasks, t => t.Id == taskId);
            Assert.DoesNotContain(workspace.TaskList.Tasks, t => t.Id == taskId);
            Assert.DoesNotContain(workspace.Canvas.Markers, m => m.Task.Id == taskId);
        }
    }
}
