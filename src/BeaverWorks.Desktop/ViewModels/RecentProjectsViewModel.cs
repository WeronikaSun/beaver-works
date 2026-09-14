using System.Collections.ObjectModel;
using System.IO;
using BeaverWorks.Core.Models;
using BeaverWorks.Core.Persistence;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Backs the post-login recent-projects screen: lists this user's
/// most-recently-used projects and offers ways to start a new one or open
/// an existing one, either from the list or by browsing the disk.
/// </summary>
public partial class RecentProjectsViewModel : ObservableObject
{
    private readonly string _username;
    private readonly IProjectStore _projectStore;
    private readonly IRecentProjectsStore _recentProjectsStore;

    [ObservableProperty]
    private string? _errorMessage;

    public ObservableCollection<RecentProjectEntry> RecentProjects { get; } = [];

    public bool HasRecentProjects => RecentProjects.Count > 0;

    public bool IsEmpty => RecentProjects.Count == 0;

    public event EventHandler? NewProjectRequested;

    public event EventHandler? BudgetSettingsRequested;

    public event EventHandler<OpenedProjectEventArgs>? ProjectOpened;

    public RecentProjectsViewModel(string username, IProjectStore projectStore, IRecentProjectsStore recentProjectsStore)
    {
        _username = username;
        _projectStore = projectStore;
        _recentProjectsStore = recentProjectsStore;

        Reload();
    }

    private void Reload()
    {
        RecentProjects.Clear();
        foreach (var entry in _recentProjectsStore.LoadRecent(_username))
        {
            RecentProjects.Add(entry);
        }

        OnPropertyChanged(nameof(HasRecentProjects));
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    private void NewProject() => NewProjectRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void BudgetSettings() => BudgetSettingsRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenProject(RecentProjectEntry entry) => TryOpen(entry.FilePath);

    [RelayCommand]
    private void OpenFromDisk()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "BeaverWorks projects (*.bwproj)|*.bwproj|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog() == true)
        {
            TryOpen(dialog.FileName);
        }
    }

    private void TryOpen(string filePath)
    {
        ErrorMessage = null;

        Project project;
        try
        {
            project = _projectStore.Load(filePath);
        }
        catch (PlanImageMissingException)
        {
            ErrorMessage = "This project's floor-plan image can't be found. Move it back next to the project file and try again.";
            return;
        }
        catch (IOException)
        {
            ErrorMessage = "This project file couldn't be opened.";
            return;
        }

        _recentProjectsStore.RecordOpened(_username, new RecentProjectEntry
        {
            Name = project.Name,
            FilePath = filePath,
        });

        ProjectOpened?.Invoke(this, new OpenedProjectEventArgs(project, filePath));
    }
}
