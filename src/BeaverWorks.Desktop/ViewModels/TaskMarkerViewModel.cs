using System.Windows.Media;
using BeaverWorks.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// One rendered marker on the plan canvas: a task's control-relative pixel
/// position (recomputed whenever the canvas is resized), the brush for its
/// current status, and whether it's the currently selected task (kept in
/// sync with the task list panel by <c>ProjectWorkspaceViewModel</c>).
/// </summary>
public partial class TaskMarkerViewModel : ObservableObject
{
    public TaskMarkerViewModel(RenovationTask task)
    {
        Task = task;
    }

    public RenovationTask Task { get; private set; }

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private bool _isSelected;

    public Brush Brush => TaskStatusColors.ForStatus(Task.Status);

    /// <summary>
    /// Replaces the underlying task (e.g. after an edit) and refreshes the
    /// bindings — such as <see cref="Brush"/> — that are derived from it.
    /// </summary>
    public void UpdateTask(RenovationTask task)
    {
        Task = task;
        OnPropertyChanged(nameof(Task));
        OnPropertyChanged(nameof(Brush));
    }
}
