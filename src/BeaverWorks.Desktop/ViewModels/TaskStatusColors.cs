using System.Windows.Media;
using BeaverWorks.Core.Models;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Centralizes the FR-008 status→color rule so the canvas marker rendering
/// and any future status-change UI (S-02) share one definition. Colors are
/// chosen to be distinguishable at a glance without relying on red/green
/// alone.
/// </summary>
public static class TaskStatusColors
{
    public static Brush ForStatus(RenovationTaskStatus status) => status switch
    {
        RenovationTaskStatus.Planned => Brushes.DodgerBlue,
        RenovationTaskStatus.Active => Brushes.Orange,
        RenovationTaskStatus.Blocked => Brushes.Crimson,
        RenovationTaskStatus.Done => Brushes.ForestGreen,
        _ => Brushes.Gray,
    };
}
