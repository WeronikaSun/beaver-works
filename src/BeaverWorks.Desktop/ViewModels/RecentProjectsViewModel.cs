using CommunityToolkit.Mvvm.ComponentModel;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Backs the post-login recent-projects screen. Intentionally empty-state
/// only for this slice — S-01 replaces this with a real project list.
/// </summary>
public partial class RecentProjectsViewModel : ObservableObject
{
    public string EmptyStateMessage => "No projects yet.";
}
