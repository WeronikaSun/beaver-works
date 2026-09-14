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
    private IUserBudgetProfileStore? _budgetProfileStore;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var credentialStore = new CredentialStore();
        _authService = new AuthService(credentialStore);
        _userSession = new UserSession();
        _projectStore = new ProjectStore();
        _recentProjectsStore = new RecentProjectsStore();
        _budgetProfileStore = new UserBudgetProfileStore();

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
        recentProjectsViewModel.BudgetSettingsRequested += (_, _) => ShowBudgetSettingsDialog();
        recentProjectsViewModel.ProjectOpened += (_, args) => ShowPlanCanvas(args);

        _mainWindow!.Content = new RecentProjectsView { DataContext = recentProjectsViewModel };
    }

    private void ShowBudgetSettingsDialog()
    {
        var budgetSettingsViewModel = new BudgetSettingsViewModel(_userSession!.CurrentUsername!, _budgetProfileStore!);

        var dialog = new BudgetSettingsDialog(budgetSettingsViewModel) { Owner = _mainWindow };
        dialog.ShowDialog();
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
        var workspaceViewModel = new ProjectWorkspaceViewModel(args.Project, args.FilePath, _projectStore!, _userSession!.CurrentUsername!, _budgetProfileStore!);
        workspaceViewModel.NewTaskRequested += (_, position) => ShowNewTaskDialog(workspaceViewModel, position);
        workspaceViewModel.TaskList.EditRequested += (_, taskId) => ShowEditTaskDialog(workspaceViewModel, taskId);
        workspaceViewModel.TaskList.DeleteRequested += (_, taskId) => ConfirmAndDeleteTask(workspaceViewModel, taskId);
        workspaceViewModel.SaveFailed += (_, message) => MessageBox.Show(
            _mainWindow,
            $"The change couldn't be saved and was reverted: {message}",
            "Save failed",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        _mainWindow!.Content = new ProjectWorkspaceView { DataContext = workspaceViewModel };
    }

    private void ShowNewTaskDialog(ProjectWorkspaceViewModel workspaceViewModel, PlanPoint position)
    {
        var newTaskViewModel = new NewTaskViewModel(position);
        newTaskViewModel.TaskCreated += (_, task) => workspaceViewModel.AddTask(task);

        var dialog = new NewTaskDialog(newTaskViewModel) { Owner = _mainWindow };
        dialog.ShowDialog();
    }

    private void ShowEditTaskDialog(ProjectWorkspaceViewModel workspaceViewModel, Guid taskId)
    {
        var task = workspaceViewModel.TaskList.Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is null)
        {
            return;
        }

        var editTaskViewModel = new EditTaskViewModel(task, workspaceViewModel.TaskList.Tasks, workspaceViewModel.Project);
        editTaskViewModel.TaskUpdated += (_, updated) => workspaceViewModel.UpdateTask(updated);

        var dialog = new EditTaskDialog(editTaskViewModel) { Owner = _mainWindow };
        dialog.ShowDialog();
    }

    private void ConfirmAndDeleteTask(ProjectWorkspaceViewModel workspaceViewModel, Guid taskId)
    {
        var task = workspaceViewModel.TaskList.Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is null)
        {
            return;
        }

        var dependents = workspaceViewModel.Project.GetDependents(taskId);
        if (dependents.Count > 0)
        {
            MessageBox.Show(
                _mainWindow,
                $"This task can't be deleted because the following task(s) depend on it: {string.Join(", ", dependents.Select(t => t.Title))}",
                "Delete blocked",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            _mainWindow,
            $"Delete task \"{task.Title}\"? This cannot be undone.",
            "Delete task",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            workspaceViewModel.DeleteTask(taskId);
        }
    }
}

