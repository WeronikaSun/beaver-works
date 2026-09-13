using BeaverWorks.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Backs the "New task" dialog opened from a valid plan click. Only
/// <see cref="Title"/> is required; the other FR-007 fields are optional at
/// creation time and default per <see cref="RenovationTask"/>'s own
/// defaults. <see cref="RoomId"/> and dependencies stay unset here and are
/// edited later in S-02.
/// </summary>
public partial class NewTaskViewModel : ObservableObject
{
    private readonly PlanPoint _position;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private int _priority = RenovationTask.DefaultPriority;

    [ObservableProperty]
    private decimal? _estimatedCost;

    [ObservableProperty]
    private string? _errorMessage;

    public event EventHandler<RenovationTask>? TaskCreated;

    public event EventHandler? Cancelled;

    public NewTaskViewModel(PlanPoint position)
    {
        _position = position;
    }

    [RelayCommand]
    private void Create()
    {
        ErrorMessage = null;

        var trimmedTitle = Title.Trim();
        if (trimmedTitle.Length == 0)
        {
            ErrorMessage = "Please enter a title.";
            return;
        }

        var task = RenovationTask.Create(
            trimmedTitle,
            _position,
            description: string.IsNullOrWhiteSpace(Description) ? null : Description,
            priority: Priority,
            estimatedCost: EstimatedCost);

        TaskCreated?.Invoke(this, task);
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);
}
