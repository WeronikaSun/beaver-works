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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private RenovationTask? _selectedTask;

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
    /// <see cref="SelectedTask"/> by Id if it's still present.
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
    /// Sets <see cref="SelectedTask"/> by Id without raising
    /// <see cref="SelectionChanged"/> — used when selection is driven
    /// externally (e.g. a canvas marker click) to avoid a sync loop.
    /// </summary>
    public void Select(Guid? taskId)
    {
        _suppressSelectionChanged = true;
        try
        {
            SelectedTask = taskId is null ? null : Tasks.FirstOrDefault(t => t.Id == taskId);
        }
        finally
        {
            _suppressSelectionChanged = false;
        }
    }

    partial void OnSelectedTaskChanged(RenovationTask? value)
    {
        OnPropertyChanged(nameof(SelectedTaskDependencySummary));
        OnPropertyChanged(nameof(SelectedTaskEstimatedCostDisplay));
        OnPropertyChanged(nameof(SelectedTaskEstimatedTimeDisplay));

        if (_suppressSelectionChanged)
        {
            return;
        }

        SelectionChanged?.Invoke(this, value?.Id);
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
