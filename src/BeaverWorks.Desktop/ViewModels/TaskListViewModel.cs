using System.Collections.ObjectModel;
using BeaverWorks.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Backs the side panel docked next to the plan canvas: the list of a
/// project's tasks, the selected task's full detail readout, and a quick
/// status-change dropdown. Selection is kept in sync with the canvas's
/// markers by <see cref="ProjectWorkspaceViewModel"/>, which owns both this
/// and <see cref="PlanCanvasViewModel"/>.
/// </summary>
public partial class TaskListViewModel : ObservableObject
{
    private bool _suppressSelectionChanged;

    public ObservableCollection<RenovationTask> Tasks { get; } = [];

    /// <summary>The task list sorted for display (FR-014): recommended tasks first, then
    /// by priority ascending, then by <see cref="RenovationTask.CreatedAt"/> ascending —
    /// each paired with its recommendation/exclusion label. Rebuilt wholesale by
    /// <see cref="UpdateRecommendations"/> whenever recommendations are recomputed.</summary>
    public ObservableCollection<TaskListRowViewModel> Rows { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private TaskListRowViewModel? _selectedRow;

    /// <summary>A persistent header summary of the remaining time/money budget (FR-013), sourced
    /// from the same profile passed to the most recent <see cref="UpdateRecommendations"/> call.</summary>
    [ObservableProperty]
    private string _budgetSummaryText = string.Empty;

    /// <summary>The currently selected task, derived from <see cref="SelectedRow"/> so existing
    /// consumers (the detail panel, <c>App.xaml.cs</c>, <see cref="EditTaskViewModel"/>) keep working unchanged.</summary>
    public RenovationTask? SelectedTask => SelectedRow?.Task;

    /// <summary>The full set of statuses shown in the quick status-change dropdown.</summary>
    public IReadOnlyList<RenovationTaskStatus> StatusOptions { get; } = Enum.GetValues<RenovationTaskStatus>();

    /// <summary>A short, human-readable summary of the selected task's dependencies, resolved to titles.</summary>
    public string SelectedTaskDependencySummary =>
        SelectedTask is null || SelectedTask.DependsOnTaskIds.Count == 0
            ? "None"
            : string.Join(", ", SelectedTask.DependsOnTaskIds
                .Select(id => Tasks.FirstOrDefault(t => t.Id == id)?.Title ?? "(unknown)"));

    /// <summary>The selected task's estimated cost, formatted for display, or a placeholder when unset.</summary>
    public string SelectedTaskEstimatedCostDisplay =>
        SelectedTask?.EstimatedCost is { } cost ? cost.ToString("C") : "—";

    /// <summary>The selected task's estimated time, formatted for display, or a placeholder when unset.</summary>
    public string SelectedTaskEstimatedTimeDisplay =>
        SelectedTask?.EstimatedTime is { } time ? time.ToString("g") : "—";

    /// <summary>The selected task's room, or a placeholder when unset.</summary>
    public string SelectedTaskRoomDisplay =>
        string.IsNullOrWhiteSpace(SelectedTask?.RoomId) ? "—" : SelectedTask.RoomId;

    /// <summary>Raised when the selection changes from user interaction with the list itself — not
    /// when selection is driven externally via <see cref="Select"/>, which avoids a canvas↔list sync loop.</summary>
    public event EventHandler<Guid?>? SelectionChanged;

    /// <summary>Raised with the selected task's Id and the newly chosen status from the quick dropdown.</summary>
    public event EventHandler<(Guid TaskId, RenovationTaskStatus NewStatus)>? StatusChangeRequested;

    /// <summary>Raised with the selected task's Id when "Edit" is invoked.</summary>
    public event EventHandler<Guid>? EditRequested;

    /// <summary>Raised with the selected task's Id when "Delete" is invoked.</summary>
    public event EventHandler<Guid>? DeleteRequested;

    public TaskListViewModel(IEnumerable<RenovationTask> initialTasks)
    {
        Refresh(initialTasks);
    }

    /// <summary>
    /// Replaces the list's contents, re-selecting the previous
    /// <see cref="SelectedTask"/> by Id if it's still present. Note this
    /// only refreshes the raw <see cref="Tasks"/> collection — callers
    /// must follow up with <see cref="UpdateRecommendations"/> to rebuild
    /// <see cref="Rows"/> against the latest budget profile.
    /// </summary>
    public void Refresh(IEnumerable<RenovationTask> tasks)
    {
        var previousSelectedId = SelectedTask?.Id;

        Tasks.Clear();
        foreach (var task in tasks)
        {
            Tasks.Add(task);
        }

        Select(previousSelectedId);
    }

    /// <summary>
    /// Rebuilds <see cref="Rows"/> from the current <see cref="Tasks"/> and
    /// <paramref name="recommendations"/> (FR-014), ordered recommended-first,
    /// then by priority ascending, then by <see cref="RenovationTask.CreatedAt"/>
    /// ascending, and refreshes <see cref="BudgetSummaryText"/> from
    /// <paramref name="profile"/>. Preserves the current selection by Id.
    /// </summary>
    public void UpdateRecommendations(IReadOnlyList<TaskRecommendation> recommendations, UserBudgetProfile profile)
    {
        var previousSelectedId = SelectedTask?.Id;
        var recommendationsByTaskId = recommendations.ToDictionary(r => r.TaskId);

        var orderedRows = Tasks
            .Select(task =>
            {
                var recommendation = recommendationsByTaskId.TryGetValue(task.Id, out var found)
                    ? found
                    : new TaskRecommendation(task.Id, false, string.Empty);

                return new TaskListRowViewModel(task, recommendation.Label, recommendation.IsRecommended);
            })
            .OrderByDescending(row => row.IsRecommended)
            .ThenBy(row => row.Task.Priority)
            .ThenBy(row => row.Task.CreatedAt);

        Rows.Clear();
        foreach (var row in orderedRows)
        {
            Rows.Add(row);
        }

        BudgetSummaryText = $"Remaining: {profile.RemainingTimeHours:0.#}h this week · {profile.RemainingMoney:C} this month";

        Select(previousSelectedId);
    }

    /// <summary>
    /// Sets <see cref="SelectedRow"/> by task Id (looked up in <see cref="Rows"/>)
    /// without raising <see cref="SelectionChanged"/> — used when selection is
    /// driven externally (e.g. a canvas marker click) to avoid a sync loop.
    /// </summary>
    public void Select(Guid? taskId)
    {
        _suppressSelectionChanged = true;
        try
        {
            SelectedRow = taskId is null ? null : Rows.FirstOrDefault(r => r.Task.Id == taskId);
        }
        finally
        {
            _suppressSelectionChanged = false;
        }
    }

    partial void OnSelectedRowChanged(TaskListRowViewModel? value)
    {
        OnPropertyChanged(nameof(SelectedTask));
        OnPropertyChanged(nameof(SelectedTaskDependencySummary));
        OnPropertyChanged(nameof(SelectedTaskEstimatedCostDisplay));
        OnPropertyChanged(nameof(SelectedTaskEstimatedTimeDisplay));
        OnPropertyChanged(nameof(SelectedTaskRoomDisplay));

        if (_suppressSelectionChanged)
        {
            return;
        }

        SelectionChanged?.Invoke(this, value?.Task.Id);
    }

    [RelayCommand]
    private void ChangeStatus(RenovationTaskStatus newStatus)
    {
        if (SelectedTask is null || SelectedTask.Status == newStatus)
        {
            return;
        }

        StatusChangeRequested?.Invoke(this, (SelectedTask.Id, newStatus));
    }

    [RelayCommand(CanExecute = nameof(HasSelectedTask))]
    private void Edit() => EditRequested?.Invoke(this, SelectedTask!.Id);

    [RelayCommand(CanExecute = nameof(HasSelectedTask))]
    private void Delete() => DeleteRequested?.Invoke(this, SelectedTask!.Id);

    private bool HasSelectedTask => SelectedTask is not null;
}
