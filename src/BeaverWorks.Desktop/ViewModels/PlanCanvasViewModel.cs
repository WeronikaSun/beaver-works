using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using BeaverWorks.Core.Models;
using BeaverWorks.Core.Persistence;
using BeaverWorks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Backs the floor-plan canvas: shows the open project's plan image with
/// its tasks overlaid as status-colored markers, and turns a valid click on
/// the image into a new-task request at the clicked normalized position.
/// Marker positions are recomputed from <see cref="PlanCoordinateMapper"/>
/// whenever the hosting control is resized, so they stay anchored to the
/// image rather than drifting independently.
/// </summary>
public partial class PlanCanvasViewModel : ObservableObject
{
    private readonly Project _project;
    private readonly string _projectFilePath;
    private readonly IProjectStore _projectStore;

    private double _controlWidth;
    private double _controlHeight;

    public string ProjectName => _project.Name;

    public BitmapImage PlanImage { get; }

    public ObservableCollection<TaskMarkerViewModel> Markers { get; } = [];

    /// <summary>Raised with the clicked plan position when a valid (non-margin) click occurs.</summary>
    public event EventHandler<PlanPoint>? NewTaskRequested;

    public PlanCanvasViewModel(Project project, string projectFilePath, IProjectStore projectStore)
    {
        _project = project;
        _projectFilePath = projectFilePath;
        _projectStore = projectStore;

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
    /// Adds a newly created task to the open project, persists it
    /// immediately, and adds its marker without re-rendering the rest of
    /// the canvas.
    /// </summary>
    public void AddTask(RenovationTask task)
    {
        _project.Tasks.Add(task);
        _project.UpdatedAt = DateTimeOffset.UtcNow;
        _projectStore.Save(_project, _projectFilePath);

        var marker = new TaskMarkerViewModel(task);
        Markers.Add(marker);
        RecomputeMarkerPositions();
    }
}
