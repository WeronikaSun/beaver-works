using System.Configuration;
using System.Data;
using System.Windows;
using BeaverWorks.Core.Models;
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
    private IProjectStore? _projectStore;
    private IRecentProjectsStore? _recentProjectsStore;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var credentialStore = new CredentialStore();
        _authService = new AuthService(credentialStore);
        _userSession = new UserSession();
        _projectStore = new ProjectStore();
        _recentProjectsStore = new RecentProjectsStore();

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
        var recentProjectsViewModel = new RecentProjectsViewModel(_userSession!.CurrentUsername!, _projectStore!, _recentProjectsStore!);
        recentProjectsViewModel.NewProjectRequested += (_, _) => ShowNewProjectDialog();
        recentProjectsViewModel.ProjectOpened += (_, args) => ShowPlanCanvas(args);

        _mainWindow!.Content = new RecentProjectsView { DataContext = recentProjectsViewModel };
    }

    private void ShowNewProjectDialog()
    {
        var newProjectViewModel = new NewProjectViewModel(_userSession!.CurrentUsername!, _projectStore!, _recentProjectsStore!);

        OpenedProjectEventArgs? created = null;
        newProjectViewModel.ProjectCreated += (_, args) => created = args;

        var dialog = new NewProjectDialog(newProjectViewModel) { Owner = _mainWindow };
        var result = dialog.ShowDialog();

        if (result == true && created is not null)
        {
            ShowPlanCanvas(created);
        }
    }

    private void ShowPlanCanvas(OpenedProjectEventArgs args)
    {
        var canvasViewModel = new PlanCanvasViewModel(args.Project, args.FilePath, _projectStore!);
        canvasViewModel.NewTaskRequested += (_, position) => ShowNewTaskDialog(canvasViewModel, position);

        _mainWindow!.Content = new PlanCanvasView { DataContext = canvasViewModel };
    }

    private void ShowNewTaskDialog(PlanCanvasViewModel canvasViewModel, PlanPoint position)
    {
        var newTaskViewModel = new NewTaskViewModel(position);
        newTaskViewModel.TaskCreated += (_, task) => canvasViewModel.AddTask(task);

        var dialog = new NewTaskDialog(newTaskViewModel) { Owner = _mainWindow };
        dialog.ShowDialog();
    }
}

