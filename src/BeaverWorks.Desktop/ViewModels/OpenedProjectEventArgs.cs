using BeaverWorks.Core.Models;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Carries the project that was just created or opened, plus the
/// <c>.bwproj</c> file path it was loaded from/saved to — shared by
/// <see cref="NewProjectViewModel"/> and <see cref="RecentProjectsViewModel"/>
/// so <c>App.xaml.cs</c> can navigate to the same place regardless of which
/// flow produced the open project.
/// </summary>
public sealed class OpenedProjectEventArgs(Project project, string filePath) : EventArgs
{
    public Project Project { get; } = project;

    public string FilePath { get; } = filePath;
}
