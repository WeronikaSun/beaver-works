using BeaverWorks.Core.Persistence;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Backs the "Budget settings" dialog: lets the user declare/edit their
/// weekly time and monthly money budgets (FR-013). Mirrors
/// <see cref="NewProjectViewModel"/>'s <c>Save</c>/<c>Cancel</c> event
/// pattern so <c>BudgetSettingsDialog.xaml.cs</c> wires up identically to
/// <c>NewProjectDialog.xaml.cs</c>.
/// </summary>
public partial class BudgetSettingsViewModel : ObservableObject
{
    private readonly string _username;
    private readonly IUserBudgetProfileStore _store;

    [ObservableProperty]
    private decimal _weeklyTimeBudgetHours;

    [ObservableProperty]
    private decimal _monthlyMoneyBudget;

    [ObservableProperty]
    private string? _errorMessage;

    public event EventHandler? BudgetUpdated;

    public event EventHandler? Cancelled;

    public BudgetSettingsViewModel(string username, IUserBudgetProfileStore store)
    {
        _username = username;
        _store = store;

        var profile = _store.Load(_username);
        _weeklyTimeBudgetHours = profile.WeeklyTimeBudgetHours;
        _monthlyMoneyBudget = profile.MonthlyMoneyBudget;
    }

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = null;

        if (WeeklyTimeBudgetHours < 0)
        {
            ErrorMessage = "Weekly time budget cannot be negative.";
            return;
        }

        if (MonthlyMoneyBudget < 0)
        {
            ErrorMessage = "Monthly money budget cannot be negative.";
            return;
        }

        var profile = _store.Load(_username);
        profile.WeeklyTimeBudgetHours = WeeklyTimeBudgetHours;
        profile.MonthlyMoneyBudget = MonthlyMoneyBudget;
        _store.Save(_username, profile);

        BudgetUpdated?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);
}
