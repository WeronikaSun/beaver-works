using System.Text.Json;
using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Persistence;

/// <summary>
/// JSON-file-backed <see cref="IProjectStore"/>, mirroring
/// <see cref="CredentialStore"/>'s pattern. Project files use a
/// <c>.bwproj</c> extension by convention (not enforced here — the caller
/// supplies the full path).
/// </summary>
public sealed class ProjectStore : IProjectStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public Project Load(string projectFilePath)
    {
        var json = File.ReadAllText(projectFilePath);
        var project = JsonSerializer.Deserialize<Project>(json, SerializerOptions)
            ?? throw new InvalidDataException($"Project file is empty or invalid: {projectFilePath}");

        if (!File.Exists(project.PlanImagePath))
        {
            throw new PlanImageMissingException(project.PlanImagePath);
        }

        return project;
    }

    public void Save(Project project, string projectFilePath)
    {
        var directory = Path.GetDirectoryName(projectFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(project, SerializerOptions);
        File.WriteAllText(projectFilePath, json);
    }
}
