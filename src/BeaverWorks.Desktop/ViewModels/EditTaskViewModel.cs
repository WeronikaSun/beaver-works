using System.Collections.ObjectModel;
using BeaverWorks.Core.Models;
using BeaverWorks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// One selectable entry in the edit dialog's dependency checklist: another
/// task in the project the edited task could depend on, plus whether it's
/// currently checked.
/// </summary>
public partial class TaskDependencyOption : ObservableObject
{
    public Guid Id { get; }

    public string Title { get; }

    [ObservableProperty]
    private bool _isSelected;

    public TaskDependencyOption(Guid id, string title, bool isSelected)
    {
        Id = id;
        Title = title;
        _isSelected = isSelected;
    }
}

/// <summary>
/// Backs the "Edit task" dialog, pre-populated from an existing task and
/// covering every FR-007 field plus dependency selection — unlike
/// <see cref="NewTaskViewModel"/>, which only covers title/description/
/// priority/cost at creation time. Mirrors its <c>Create</c>/<c>Cancel</c>
/// event pattern (<see cref="TaskUpdated"/>/<see cref="Cancelled"/>) so
/// <c>EditTaskDialog.xaml.cs</c> can wire up identically to
/// <c>NewTaskDialog.xaml.cs</c>.
/// </summary>
public partial class EditTaskViewModel : ObservableObject
{
    private readonly RenovationTask _task;
    private readonly Project _project;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private RenovationTaskStatus _status;

    [ObservableProperty]
    private int _priority;

    [ObservableProperty]
    private decimal? _estimatedCost;

    /// <summary>The task's estimated time expressed in hours for editing; converted to/from <see cref="TimeSpan"/> on save.</summary>
    [ObservableProperty]
    private decimal? _estimatedTimeHours;

    [ObservableProperty]
    private string? _roomId;

    [ObservableProperty]
    private string? _errorMessage;

    public IReadOnlyList<RenovationTaskStatus> StatusOptions { get; } = Enum.GetValues<RenovationTaskStatus>();

    /// <summary>Every other task in the project, selectable as a dependency; entries in <see cref="RenovationTask.DependsOnTaskIds"/> start checked.</summary>
    public ObservableCollection<TaskDependencyOption> DependencyOptions { get; }

    public event EventHandler<RenovationTask>? TaskUpdated;

    public event EventHandler? Cancelled;

    public EditTaskViewModel(RenovationTask task, IReadOnlyList<RenovationTask> allProjectTasks, Project project)
    {
        _task = task;
        _project = project;

        _title = task.Title;
        _description = task.Description;
        _status = task.Status;
        _priority = task.Priority;
        _estimatedCost = task.EstimatedCost;
        _estimatedTimeHours = task.EstimatedTime is { } time ? (decimal)time.TotalHours : null;
        _roomId = task.RoomId;

        DependencyOptions = new ObservableCollection<TaskDependencyOption>(
            allProjectTasks
                .Where(t => t.Id != task.Id)
                .Select(t => new TaskDependencyOption(t.Id, t.Title, task.DependsOnTaskIds.Contains(t.Id))));
    }

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = null;

        var trimmedTitle = Title.Trim();
        if (trimmedTitle.Length == 0)
        {
            ErrorMessage = "Please enter a title.";
            return;
        }

        if (Priority < RenovationTask.MinPriority || Priority > RenovationTask.MaxPriority)
        {
            ErrorMessage = $"Priority must be between {RenovationTask.MinPriority} and {RenovationTask.MaxPriority}.";
            return;
        }

        if (EstimatedCost is < 0)
        {
            ErrorMessage = "Estimated cost cannot be negative.";
            return;
        }

        if (EstimatedTimeHours is < 0)
        {
            ErrorMessage = "Estimated time cannot be negative.";
            return;
        }

        var selectedDependencyIds = DependencyOptions.Where(o => o.IsSelected).Select(o => o.Id).ToList();

        if (TaskDependencyValidator.WouldCreateCycle(_project, _task.Id, selectedDependencyIds))
        {
            ErrorMessage = "This dependency selection would create a cycle.";
            return;
        }

        var updated = new RenovationTask
        {
            Id = _task.Id,
            Title = trimmedTitle,
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
            Status = Status,
            Priority = Priority,
            EstimatedCost = EstimatedCost,
            EstimatedTime = EstimatedTimeHours is { } hours ? TimeSpan.FromHours((double)hours) : null,
            RoomId = string.IsNullOrWhiteSpace(RoomId) ? null : RoomId,
            Position = _task.Position,
            DependsOnTaskIds = selectedDependencyIds,
            CreatedAt = _task.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        TaskUpdated?.Invoke(this, updated);
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);
}
