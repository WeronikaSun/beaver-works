using System.Collections.ObjectModel;
using BeaverWorks.Core.Models;
using BeaverWorks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Backs the "New task" dialog opened from a valid plan click. Covers
/// every field <see cref="EditTaskViewModel"/> does except
/// <see cref="RenovationTask.Status"/>, which stays derived rather than a
/// free choice at creation time (see <see cref="TaskDependencyStatusResolver"/>):
/// a task created with an unmet dependency is saved as
/// <see cref="RenovationTaskStatus.Blocked"/>, otherwise it starts
/// <see cref="RenovationTaskStatus.Planned"/>.
/// </summary>
public partial class NewTaskViewModel : ObservableObject
{
    private readonly PlanPoint _position;
    private readonly IReadOnlyList<RenovationTask> _allProjectTasks;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private int _priority = RenovationTask.DefaultPriority;

    [ObservableProperty]
    private decimal? _estimatedCost;

    /// <summary>The task's estimated time expressed in hours for editing; converted to/from <see cref="TimeSpan"/> on save.</summary>
    [ObservableProperty]
    private decimal? _estimatedTimeHours;

    [ObservableProperty]
    private string? _roomId;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Every task in the project selectable as a dependency (excludes <see cref="RenovationTaskStatus.Done"/> tasks — there's no self to exclude yet, since this task doesn't exist).</summary>
    public ObservableCollection<TaskDependencyOption> DependencyOptions { get; }

    public event EventHandler<RenovationTask>? TaskCreated;

    public event EventHandler? Cancelled;

    public NewTaskViewModel(PlanPoint position, IReadOnlyList<RenovationTask> allProjectTasks)
    {
        _position = position;
        _allProjectTasks = allProjectTasks;

        DependencyOptions = new ObservableCollection<TaskDependencyOption>(
            TaskDependencyStatusResolver.GetSelectableDependencies(allProjectTasks, excludeTaskId: null)
                .Select(t => new TaskDependencyOption(t.Id, t.Title, isSelected: false)));
    }

    [RelayCommand]
    private void Create()
    {
        ErrorMessage = null;

        var titleError = TaskFieldValidator.ValidateTitle(Title);
        if (titleError is not null)
        {
            ErrorMessage = titleError;
            return;
        }

        var priorityError = TaskFieldValidator.ValidatePriority(Priority);
        if (priorityError is not null)
        {
            ErrorMessage = priorityError;
            return;
        }

        var costError = TaskFieldValidator.ValidateEstimatedCost(EstimatedCost);
        if (costError is not null)
        {
            ErrorMessage = costError;
            return;
        }

        if (!TaskFieldValidator.TryResolveEstimatedTime(EstimatedTimeHours, out var estimatedTime, out var timeError))
        {
            ErrorMessage = timeError;
            return;
        }

        var selectedDependencyIds = DependencyOptions.Where(o => o.IsSelected).Select(o => o.Id).ToList();

        var task = RenovationTask.Create(
            Title.Trim(),
            _position,
            description: string.IsNullOrWhiteSpace(Description) ? null : Description,
            priority: Priority,
            estimatedCost: EstimatedCost,
            estimatedTime: estimatedTime,
            roomId: string.IsNullOrWhiteSpace(RoomId) ? null : RoomId);

        task.DependsOnTaskIds = selectedDependencyIds;
        task.Status = TaskDependencyStatusResolver.ResolveStatus(RenovationTaskStatus.Planned, selectedDependencyIds, _allProjectTasks);

        TaskCreated?.Invoke(this, task);
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);
}
