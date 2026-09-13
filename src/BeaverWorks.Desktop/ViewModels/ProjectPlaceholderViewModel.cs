using BeaverWorks.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Temporary stand-in for the floor-plan canvas screen (Phase 3). Shows just
/// enough about the opened project to manually verify that project
/// creation/opening and persistence round-trip correctly, ahead of the real
/// canvas being built.
/// </summary>
public partial class ProjectPlaceholderViewModel : ObservableObject
{
    public ProjectPlaceholderViewModel(Project project, string filePath)
    {
        ProjectName = project.Name;
        FilePath = filePath;
        TaskCount = project.Tasks.Count;
    }

    public string ProjectName { get; }

    public string FilePath { get; }

    public int TaskCount { get; }
}
