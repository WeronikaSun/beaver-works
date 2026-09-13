using System.Configuration;
using System.Data;
using System.Windows;
using BeaverWorks.Core.Persistence;
using BeaverWorks.Core.Services;
using BeaverWorks.Desktop.ViewModels;
using BeaverWorks.Desktop.Views;

namespace BeaverWorks.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private AuthService? _authService;
    private UserSession? _userSession;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var credentialStore = new CredentialStore();
        _authService = new AuthService(credentialStore);
        _userSession = new UserSession();

        _mainWindow = new MainWindow();
        MainWindow = _mainWindow;

        ShowLogin();

        _mainWindow.Show();
    }

    private void ShowLogin()
    {
        var loginViewModel = new LoginViewModel(_authService!, _userSession!);
        loginViewModel.LoginSucceeded += (_, _) => ShowRecentProjects();

        _mainWindow!.Content = new LoginView { DataContext = loginViewModel };
    }

    private void ShowRecentProjects()
    {
        var recentProjectsViewModel = new RecentProjectsViewModel();
        _mainWindow!.Content = new RecentProjectsView { DataContext = recentProjectsViewModel };
    }
}

