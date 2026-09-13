using System.Windows.Media;
using BeaverWorks.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// One rendered marker on the plan canvas: a task's control-relative pixel
/// position (recomputed whenever the canvas is resized) plus the brush for
/// its current status.
/// </summary>
public partial class TaskMarkerViewModel(RenovationTask task) : ObservableObject
{
    public RenovationTask Task { get; } = task;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    public Brush Brush => TaskStatusColors.ForStatus(Task.Status);
}
