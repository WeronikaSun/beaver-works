using System.IO;
using BeaverWorks.Core.Models;
using BeaverWorks.Core.Persistence;
using BeaverWorks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Composition root for an open project's workspace: owns the project's
/// persistence lifecycle and wires the two panels — the plan canvas and the
/// task list — together so their selections and edits stay in sync.
/// Neither child view model talks to <see cref="IProjectStore"/> directly;
/// every mutation flows through this class so there's a single point that
/// saves and refreshes both panels consistently. Also owns the
/// budget-recommendation recompute (FR-014) and consumption (FR-015)
/// hooks: every successful save recomputes recommendations against the
/// latest budget profile, and a transition to <see cref="RenovationTaskStatus.Done"/>
/// deducts that task's estimate from the profile.
/// </summary>
public partial class ProjectWorkspaceViewModel : ObservableObject
{
    private readonly Project _project;
    private readonly string _projectFilePath;
    private readonly IProjectStore _projectStore;
    private readonly string _username;
    private readonly IUserBudgetProfileStore _budgetProfileStore;

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

    public ProjectWorkspaceViewModel(Project project, string projectFilePath, IProjectStore projectStore, string username, IUserBudgetProfileStore budgetProfileStore)
    {
        _project = project;
        _projectFilePath = projectFilePath;
        _projectStore = projectStore;
        _username = username;
        _budgetProfileStore = budgetProfileStore;

        Canvas = new PlanCanvasViewModel(project);
        TaskList = new TaskListViewModel(project.Tasks);

        Canvas.NewTaskRequested += (_, position) => NewTaskRequested?.Invoke(this, position);
        Canvas.MarkerSelected += (_, taskId) => TaskList.Select(taskId);
        TaskList.SelectionChanged += (_, taskId) => Canvas.SelectMarker(taskId);
        TaskList.StatusChangeRequested += (_, change) => UpdateTaskStatus(change.TaskId, change.NewStatus);

        RecomputeRecommendations();
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
        RecomputeRecommendations();
    }

    /// <summary>
    /// Replaces an existing task with an updated copy, persists the
    /// change, and refreshes both panels so the marker and list stay
    /// consistent with the new data. If this transitions the task to
    /// <see cref="RenovationTaskStatus.Done"/> from some other status, the
    /// task's effective time/cost is deducted from the user's budget
    /// profile after the project save succeeds (FR-015); a failure while
    /// saving the budget profile reports through <see cref="SaveFailed"/>
    /// but does not revert the already-saved task.
    /// </summary>
    public void UpdateTask(RenovationTask task) => UpdateTask(task, previousStatusOverride: null);

    private void UpdateTask(RenovationTask task, RenovationTaskStatus? previousStatusOverride)
    {
        var index = _project.Tasks.FindIndex(t => t.Id == task.Id);
        if (index < 0)
        {
            return;
        }

        // Falls back to the stored task's status when no override is given.
        // The quick status-change dropdown (see UpdateTaskStatus) mutates the
        // task in place before calling this method, so by that point the
        // stored task and the incoming task would be the same reference —
        // an override is required there to still detect the transition.
        var previousStatus = previousStatusOverride ?? _project.Tasks[index].Status;
        var previous = _project.Tasks[index];
        _project.Tasks[index] = task;

        // A task transitioning into Done may unblock other tasks that list
        // it as a dependency. Re-resolve those dependents' status here so
        // the unblock lands in the same save as the transition, instead of
        // requiring each dependent to be individually re-opened and
        // re-saved via the Edit dialog.
        var cascaded = new List<(RenovationTask Task, RenovationTaskStatus PreviousStatus, DateTimeOffset PreviousUpdatedAt)>();
        if (task.Status == RenovationTaskStatus.Done && previousStatus != RenovationTaskStatus.Done)
        {
            foreach (var candidate in _project.Tasks)
            {
                if (candidate.Id == task.Id
                    || candidate.Status != RenovationTaskStatus.Blocked
                    || !candidate.DependsOnTaskIds.Contains(task.Id))
                {
                    continue;
                }

                var resolvedStatus = TaskDependencyStatusResolver.ResolveStatusOnEdit(
                    candidate.Status, candidate.Status, candidate.DependsOnTaskIds, _project.Tasks);

                if (resolvedStatus == candidate.Status)
                {
                    continue;
                }

                cascaded.Add((candidate, candidate.Status, candidate.UpdatedAt));
                candidate.Status = resolvedStatus;
                candidate.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        _project.UpdatedAt = DateTimeOffset.UtcNow;

        if (!TrySave())
        {
            _project.Tasks[index] = previous;
            foreach (var (cascadedTask, cascadedPreviousStatus, cascadedPreviousUpdatedAt) in cascaded)
            {
                cascadedTask.Status = cascadedPreviousStatus;
                cascadedTask.UpdatedAt = cascadedPreviousUpdatedAt;
            }
            return;
        }

        Canvas.UpdateMarker(task);
        TaskList.Refresh(_project.Tasks);

        if (task.Status == RenovationTaskStatus.Done && previousStatus != RenovationTaskStatus.Done)
        {
            ApplyBudgetConsumption(task);
        }

        RecomputeRecommendations();
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
        RecomputeRecommendations();
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

        var previousStatus = task.Status;
        task.Status = newStatus;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        UpdateTask(task, previousStatus);
    }

    /// <summary>
    /// Deducts <paramref name="completedTask"/>'s effective time/cost from
    /// the user's budget profile and persists it, reporting a failure via
    /// <see cref="SaveFailed"/> without reverting the already-saved task.
    /// </summary>
    private void ApplyBudgetConsumption(RenovationTask completedTask)
    {
        var profile = _budgetProfileStore.Load(_username);
        BudgetConsumptionService.ApplyCompletion(profile, completedTask, _project.Tasks);

        try
        {
            _budgetProfileStore.Save(_username, profile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SaveFailed?.Invoke(this, ex.Message);
        }
    }

    /// <summary>
    /// Loads the current budget profile and pushes freshly computed
    /// recommendations into <see cref="TaskList"/> (FR-014).
    /// </summary>
    private void RecomputeRecommendations()
    {
        var profile = _budgetProfileStore.Load(_username);
        var recommendations = TaskRecommendationEngine.Recommend(_project.Tasks, profile);
        TaskList.UpdateRecommendations(recommendations, profile);
    }
}
