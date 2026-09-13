using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using BeaverWorks.Core.Models;
using BeaverWorks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Backs the floor-plan canvas: shows the open project's plan image with
/// its tasks overlaid as status-colored markers, turns a valid click on
/// empty plan space into a new-task request, and turns a click on an
/// existing marker into a selection request. Persistence and cross-panel
/// sync are owned by <see cref="ProjectWorkspaceViewModel"/> — this view
/// model is UI-state-only (markers, layout, click routing). Marker
/// positions are recomputed from <see cref="PlanCoordinateMapper"/>
/// whenever the hosting control is resized, so they stay anchored to the
/// image rather than drifting independently.
/// </summary>
public partial class PlanCanvasViewModel : ObservableObject
{
    private readonly Project _project;

    private double _controlWidth;
    private double _controlHeight;

    public string ProjectName => _project.Name;

    public BitmapImage PlanImage { get; }

    public ObservableCollection<TaskMarkerViewModel> Markers { get; } = [];

    /// <summary>Raised with the clicked plan position when a valid (non-margin) click occurs on empty plan space.</summary>
    public event EventHandler<PlanPoint>? NewTaskRequested;

    /// <summary>Raised with a task's Id when its marker is clicked directly.</summary>
    public event EventHandler<Guid>? MarkerSelected;

    public PlanCanvasViewModel(Project project)
    {
        _project = project;

        PlanImage = new BitmapImage();
        PlanImage.BeginInit();
        PlanImage.CacheOption = BitmapCacheOption.OnLoad;
        PlanImage.UriSource = new Uri(project.PlanImagePath, UriKind.Absolute);
        PlanImage.EndInit();

        foreach (var task in _project.Tasks)
        {
            Markers.Add(new TaskMarkerViewModel(task));
        }
    }

    /// <summary>
    /// Called by the view whenever the canvas control's rendered size
    /// changes (including its first layout pass), so markers can be
    /// repositioned against the new letterbox rectangle.
    /// </summary>
    public void UpdateControlSize(double width, double height)
    {
        _controlWidth = width;
        _controlHeight = height;
        RecomputeMarkerPositions();
    }

    private void RecomputeMarkerPositions()
    {
        foreach (var marker in Markers)
        {
            var mapped = PlanCoordinateMapper.MapPlanToControl(
                _controlWidth,
                _controlHeight,
                PlanImage.PixelWidth,
                PlanImage.PixelHeight,
                marker.Task.Position);

            if (mapped is { } point)
            {
                marker.X = point.X;
                marker.Y = point.Y;
            }
        }
    }

    [RelayCommand]
    private void HandleClick(Point controlPosition)
    {
        var mapped = PlanCoordinateMapper.TryMapClickToPlan(
            _controlWidth,
            _controlHeight,
            PlanImage.PixelWidth,
            PlanImage.PixelHeight,
            controlPosition.X,
            controlPosition.Y);

        if (mapped is { } position)
        {
            NewTaskRequested?.Invoke(this, position);
        }
    }

    /// <summary>
    /// Called by the view when an existing marker (rather than empty plan
    /// space) is clicked. Highlights the marker locally and notifies
    /// subscribers (the workspace view model relays this into the task
    /// list's selection).
    /// </summary>
    public void NotifyMarkerClicked(Guid taskId)
    {
        SelectMarker(taskId);
        MarkerSelected?.Invoke(this, taskId);
    }

    /// <summary>
    /// Highlights the marker for <paramref name="taskId"/> and un-highlights
    /// every other marker, without raising <see cref="MarkerSelected"/> —
    /// used when selection is driven externally (e.g. the task list) to
    /// avoid a sync loop.
    /// </summary>
    public void SelectMarker(Guid? taskId)
    {
        foreach (var marker in Markers)
        {
            marker.IsSelected = marker.Task.Id == taskId;
        }
    }

    /// <summary>
    /// Adds a marker for a newly created task without persisting it — the
    /// workspace view model owns saving.
    /// </summary>
    public void AddMarker(RenovationTask task)
    {
        Markers.Add(new TaskMarkerViewModel(task));
        RecomputeMarkerPositions();
    }

    /// <summary>
    /// Refreshes an existing marker (e.g. after a status/field edit) so its
    /// brush and bound task reflect the new data.
    /// </summary>
    public void UpdateMarker(RenovationTask task)
    {
        var marker = Markers.FirstOrDefault(m => m.Task.Id == task.Id);
        marker?.UpdateTask(task);
    }

    /// <summary>Removes a task's marker after it's been deleted.</summary>
    public void RemoveMarker(Guid taskId)
    {
        var marker = Markers.FirstOrDefault(m => m.Task.Id == taskId);
        if (marker is not null)
        {
            Markers.Remove(marker);
        }
    }
}
