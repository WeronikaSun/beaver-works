using System.IO;

namespace BeaverWorks.Desktop.Services;

/// <summary>
/// Desktop-layer conventions for where a user's projects and the built-in
/// sample plan asset live on disk. Kept out of <c>BeaverWorks.Core</c>
/// because <see cref="BeaverWorks.Core.Persistence.IProjectStore"/> takes an
/// explicit full path and has no opinion on folder layout.
/// </summary>
public static class ProjectPaths
{
    /// <summary>
    /// The flat folder holding every one of this user's <c>.bwproj</c> files
    /// (and their copied plan images), one file per project.
    /// </summary>
    public static string GetProjectsFolder(string username)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "BeaverWorks", username, "Projects");
    }

    /// <summary>
    /// Absolute path to the built-in sample floor-plan image shipped as
    /// Desktop content, resolved relative to the running app's base
    /// directory so it works from any install location.
    /// </summary>
    public static string GetBuiltInSamplePlanPath() =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "sample-floor-plan.png");

    /// <summary>
    /// Replaces characters that are invalid in a file name with an
    /// underscore, so a user-typed project name can be used directly as a
    /// file name.
    /// </summary>
    public static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string([.. name.Select(c => invalidChars.Contains(c) ? '_' : c)]);
        return sanitized.Trim();
    }
}
