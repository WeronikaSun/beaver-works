using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Persistence;

/// <summary>
/// Persists a single <see cref="Project"/> to/from a JSON file on disk.
/// </summary>
public interface IProjectStore
{
    /// <summary>
    /// Loads the project at <paramref name="projectFilePath"/>.
    /// </summary>
    /// <exception cref="PlanImageMissingException">
    /// Thrown when the loaded project's <see cref="Project.PlanImagePath"/>
    /// does not exist on disk. The project file itself is left untouched.
    /// </exception>
    Project Load(string projectFilePath);

    void Save(Project project, string projectFilePath);
}
