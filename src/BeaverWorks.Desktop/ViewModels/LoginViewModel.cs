using BeaverWorks.Core.Models;
using BeaverWorks.Core.Persistence;
using BeaverWorks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Backs the login/registration screen. Toggles between "log in" and
/// "create account" modes and submits to <see cref="AuthService"/>. In
/// register mode, also collects the two FR-013 budget fields and seeds a
/// <see cref="UserBudgetProfile"/> for the new account on success —
/// mirroring <see cref="BudgetSettingsViewModel"/>'s save flow but
/// starting from <see cref="UserBudgetProfile.CreateDefault"/> since there
/// isn't an existing profile yet.
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";
    private const string DuplicateUsernameMessage = "An account with this username already exists.";
    private const string MissingBudgetFieldsMessage = "Please enter a monthly renovation budget and weekly available renovation time, both greater than zero.";

    private readonly AuthService _authService;
    private readonly UserSession _userSession;
    private readonly IUserBudgetProfileStore _budgetProfileStore;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isRegisterMode;

    [ObservableProperty]
    private decimal? _monthlyBudget;

    [ObservableProperty]
    private decimal? _weeklyTimeBudget;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    public event EventHandler? LoginSucceeded;

    public LoginViewModel(AuthService authService, UserSession userSession, IUserBudgetProfileStore budgetProfileStore)
    {
        _authService = authService;
        _userSession = userSession;
        _budgetProfileStore = budgetProfileStore;
    }

    [RelayCommand]
    private void ToggleMode()
    {
        IsRegisterMode = !IsRegisterMode;
        ErrorMessage = null;
        StatusMessage = null;

        // Budget fields only apply to register mode; clear stale values so a
        // blank/zero/negative attempt can't accidentally reuse a prior
        // attempt's valid numbers left over in the view model.
        MonthlyBudget = null;
        WeeklyTimeBudget = null;
    }

    [RelayCommand]
    private void Submit()
    {
        ErrorMessage = null;
        StatusMessage = null;

        if (IsRegisterMode)
        {
            if (MonthlyBudget is not > 0 || WeeklyTimeBudget is not > 0)
            {
                ErrorMessage = MissingBudgetFieldsMessage;
                return;
            }

            var registerResult = _authService.Register(Username, Password);
            if (!registerResult.Succeeded)
            {
                ErrorMessage = DuplicateUsernameMessage;
                return;
            }

            var profile = UserBudgetProfile.CreateDefault();
            profile.WeeklyTimeBudgetHours = WeeklyTimeBudget!.Value;
            profile.MonthlyMoneyBudget = MonthlyBudget!.Value;
            _budgetProfileStore.Save(Username, profile);

            IsRegisterMode = false;
            MonthlyBudget = null;
            WeeklyTimeBudget = null;
            StatusMessage = "Account created — please log in.";
            return;
        }

        var loginResult = _authService.Login(Username, Password);
        if (!loginResult.Succeeded)
        {
            ErrorMessage = InvalidCredentialsMessage;
            return;
        }

        _userSession.SignIn(Username);
        LoginSucceeded?.Invoke(this, EventArgs.Empty);
    }
}
