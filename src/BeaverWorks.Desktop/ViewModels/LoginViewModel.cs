using BeaverWorks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Backs the login/registration screen. Toggles between "log in" and
/// "create account" modes and submits to <see cref="AuthService"/>.
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";
    private const string DuplicateUsernameMessage = "An account with this username already exists.";

    private readonly AuthService _authService;
    private readonly UserSession _userSession;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isRegisterMode;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    public event EventHandler? LoginSucceeded;

    public LoginViewModel(AuthService authService, UserSession userSession)
    {
        _authService = authService;
        _userSession = userSession;
    }

    [RelayCommand]
    private void ToggleMode()
    {
        IsRegisterMode = !IsRegisterMode;
        ErrorMessage = null;
        StatusMessage = null;
    }

    [RelayCommand]
    private void Submit()
    {
        ErrorMessage = null;
        StatusMessage = null;

        if (IsRegisterMode)
        {
            var registerResult = _authService.Register(Username, Password);
            if (!registerResult.Succeeded)
            {
                ErrorMessage = DuplicateUsernameMessage;
                return;
            }

            IsRegisterMode = false;
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
