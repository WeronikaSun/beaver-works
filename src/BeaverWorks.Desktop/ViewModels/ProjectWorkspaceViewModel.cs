using System.IO;
using BeaverWorks.Core.Models;
using BeaverWorks.Core.Persistence;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Composition root for an open project's workspace: owns the project's
/// persistence lifecycle and wires the two panels — the plan canvas and the
/// task list — together so their selections and edits stay in sync.
/// Neither child view model talks to <see cref="IProjectStore"/> directly;
/// every mutation flows through this class so there's a single point that
/// saves and refreshes both panels consistently.
/// </summary>
public partial class ProjectWorkspaceViewModel : ObservableObject
{
    private readonly Project _project;
    private readonly string _projectFilePath;
    private readonly IProjectStore _projectStore;

    public string ProjectName => _project.Name;

    /// <summary>The underlying project, exposed so the edit dialog can validate dependency cycles against the full task graph.</summary>
    public Project Project => _project;

    public PlanCanvasViewModel Canvas { get; }

    public TaskListViewModel TaskList { get; }

    /// <summary>Raised with a newly clicked plan position when empty plan space is clicked (relayed from Canvas, unchanged).</summary>
    public event EventHandler<PlanPoint>? NewTaskRequested;

    /// <summary>
    /// Raised when persisting a mutation to disk fails; the in-memory
    /// project has already been rolled back to its prior state by the time
    /// this fires, so the UI stays consistent with what's on disk.
    /// </summary>
    public event EventHandler<string>? SaveFailed;

    public ProjectWorkspaceViewModel(Project project, string projectFilePath, IProjectStore projectStore)
    {
        _project = project;
        _projectFilePath = projectFilePath;
        _projectStore = projectStore;

        Canvas = new PlanCanvasViewModel(project);
        TaskList = new TaskListViewModel(project.Tasks);

        Canvas.NewTaskRequested += (_, position) => NewTaskRequested?.Invoke(this, position);
        Canvas.MarkerSelected += (_, taskId) => TaskList.Select(taskId);
        TaskList.SelectionChanged += (_, taskId) => Canvas.SelectMarker(taskId);
        TaskList.StatusChangeRequested += (_, change) => UpdateTaskStatus(change.TaskId, change.NewStatus);
    }

    /// <summary>
    /// Adds a newly created task to the project, persists it, and reflects
    /// it in both the canvas and the list.
    /// </summary>
    public void AddTask(RenovationTask task)
    {
        _project.Tasks.Add(task);
        _project.UpdatedAt = DateTimeOffset.UtcNow;

        if (!TrySave())
        {
            _project.Tasks.Remove(task);
            return;
        }

        Canvas.AddMarker(task);
        TaskList.Refresh(_project.Tasks);
    }

    /// <summary>
    /// Replaces an existing task with an updated copy, persists the
    /// change, and refreshes both panels so the marker and list stay
    /// consistent with the new data.
    /// </summary>
    public void UpdateTask(RenovationTask task)
    {
        var index = _project.Tasks.FindIndex(t => t.Id == task.Id);
        if (index < 0)
        {
            return;
        }

        var previous = _project.Tasks[index];
        _project.Tasks[index] = task;
        _project.UpdatedAt = DateTimeOffset.UtcNow;

        if (!TrySave())
        {
            _project.Tasks[index] = previous;
            return;
        }

        Canvas.UpdateMarker(task);
        TaskList.Refresh(_project.Tasks);
    }

    /// <summary>
    /// Removes a task from the project, unless another task still depends
    /// on it — in which case the delete is silently ignored, leaving the
    /// project untouched. Callers are expected to pre-check
    /// <see cref="Project.GetDependents"/> and confirm with the user before
    /// calling this method (see <c>App.xaml.cs</c>'s delete flow); this
    /// check is only a defensive fallback for future direct callers.
    /// </summary>
    public void DeleteTask(Guid taskId)
    {
        var dependents = _project.GetDependents(taskId);
        if (dependents.Count > 0)
        {
            return;
        }

        var index = _project.Tasks.FindIndex(t => t.Id == taskId);
        if (index < 0)
        {
            return;
        }

        var removedTask = _project.Tasks[index];
        _project.Tasks.RemoveAt(index);
        _project.UpdatedAt = DateTimeOffset.UtcNow;

        if (!TrySave())
        {
            _project.Tasks.Insert(index, removedTask);
            return;
        }

        Canvas.RemoveMarker(taskId);
        TaskList.Refresh(_project.Tasks);
    }

    /// <summary>
    /// Persists the project to disk, reporting failure via
    /// <see cref="SaveFailed"/> instead of letting the exception propagate
    /// into a WPF command/event handler with an unmutated-looking UI.
    /// </summary>
    private bool TrySave()
    {
        try
        {
            _projectStore.Save(_project, _projectFilePath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SaveFailed?.Invoke(this, ex.Message);
            return false;
        }
    }

    private void UpdateTaskStatus(Guid taskId, RenovationTaskStatus newStatus)
    {
        var task = _project.Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is null)
        {
            return;
        }

        task.Status = newStatus;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        UpdateTask(task);
    }
}
