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

    public PlanCanvasViewModel Canvas { get; }

    public TaskListViewModel TaskList { get; }

    /// <summary>Raised with a newly clicked plan position when empty plan space is clicked (relayed from Canvas, unchanged).</summary>
    public event EventHandler<PlanPoint>? NewTaskRequested;

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
        _projectStore.Save(_project, _projectFilePath);

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

        _project.Tasks[index] = task;
        _projectStore.Save(_project, _projectFilePath);

        Canvas.UpdateMarker(task);
        TaskList.Refresh(_project.Tasks);
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
