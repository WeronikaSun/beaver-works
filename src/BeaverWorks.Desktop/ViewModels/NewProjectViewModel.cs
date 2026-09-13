using System.IO;
using BeaverWorks.Core.Models;
using BeaverWorks.Core.Persistence;
using BeaverWorks.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// Backs the "New project" dialog: the user names a project and either uses
/// the built-in sample floor plan or browses for their own image. Creating a
/// project copies the chosen image into that user's projects folder (never
/// referencing it in place), saves the new <see cref="Project"/>, and
/// records it in the recent-projects list.
/// </summary>
public partial class NewProjectViewModel : ObservableObject
{
    private readonly string _username;
    private readonly IProjectStore _projectStore;
    private readonly IRecentProjectsStore _recentProjectsStore;

    [ObservableProperty]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private bool _useBuiltInSample = true;

    [ObservableProperty]
    private string? _selectedImagePath;

    [ObservableProperty]
    private string? _errorMessage;

    public event EventHandler<OpenedProjectEventArgs>? ProjectCreated;

    public event EventHandler? Cancelled;

    public NewProjectViewModel(string username, IProjectStore projectStore, IRecentProjectsStore recentProjectsStore)
    {
        _username = username;
        _projectStore = projectStore;
        _recentProjectsStore = recentProjectsStore;
    }

    [RelayCommand]
    private void BrowseForImage()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedImagePath = dialog.FileName;
            UseBuiltInSample = false;
        }
    }

    [RelayCommand]
    private void Create()
    {
        ErrorMessage = null;

        var trimmedName = ProjectName.Trim();
        if (trimmedName.Length == 0)
        {
            ErrorMessage = "Please enter a project name.";
            return;
        }

        if (!UseBuiltInSample && string.IsNullOrWhiteSpace(SelectedImagePath))
        {
            ErrorMessage = "Please choose a floor-plan image, or use the built-in sample.";
            return;
        }

        var sourceImagePath = UseBuiltInSample ? ProjectPaths.GetBuiltInSamplePlanPath() : SelectedImagePath!;

        var projectsFolder = ProjectPaths.GetProjectsFolder(_username);
        Directory.CreateDirectory(projectsFolder);

        var sanitizedName = ProjectPaths.SanitizeFileName(trimmedName);
        var projectFilePath = Path.Combine(projectsFolder, $"{sanitizedName}.bwproj");

        if (File.Exists(projectFilePath))
        {
            ErrorMessage = "A project with this name already exists.";
            return;
        }

        var imageExtension = Path.GetExtension(sourceImagePath);
        var copiedImagePath = Path.Combine(projectsFolder, $"{sanitizedName}{imageExtension}");
        File.Copy(sourceImagePath, copiedImagePath, overwrite: false);

        var now = DateTimeOffset.Now;
        var project = new Project
        {
            Name = trimmedName,
            PlanImagePath = copiedImagePath,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _projectStore.Save(project, projectFilePath);
        _recentProjectsStore.RecordOpened(_username, new RecentProjectEntry
        {
            Name = project.Name,
            FilePath = projectFilePath,
        });

        ProjectCreated?.Invoke(this, new OpenedProjectEventArgs(project, projectFilePath));
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);
}
