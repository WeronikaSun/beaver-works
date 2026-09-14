using System;
using System.IO;
using BeaverWorks.Core.Models;
using BeaverWorks.Core.Persistence;
using BeaverWorks.Desktop.ViewModels;

namespace BeaverWorks.Desktop.Tests;

/// <summary>
/// Temporary placeholder proving the BeaverWorks.Desktop.Tests project,
/// its TFM/UseWPF setup, and its ProjectReference to BeaverWorks.Desktop
/// all wire up correctly. Replaced by the real persistence test in Phase 2.
/// </summary>
public class PlaceholderTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _planImagePath;

    public PlaceholderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"beaverworks-desktop-tests-placeholder-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        _planImagePath = Path.Combine(_tempDirectory, "plan.png");
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "Assets", "sample-floor-plan.png"),
            _planImagePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ProjectWorkspaceViewModel_ConstructsWithoutThrowing()
    {
        var project = new Project
        {
            Name = "Placeholder project",
            PlanImagePath = _planImagePath,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var projectFilePath = Path.Combine(_tempDirectory, "project.bwproj");
        var projectStore = new ProjectStore();
        var budgetProfileStore = new UserBudgetProfileStore(_tempDirectory);

        var workspace = new ProjectWorkspaceViewModel(
            project, projectFilePath, projectStore, "placeholder-user", budgetProfileStore);

        Assert.Equal("Placeholder project", workspace.ProjectName);
    }
}
