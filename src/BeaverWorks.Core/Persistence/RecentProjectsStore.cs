using System.Text.Json;
using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Persistence;

/// <summary>
/// JSON-file-backed <see cref="IRecentProjectsStore"/>. Defaults to
/// <c>%LOCALAPPDATA%\BeaverWorks\&lt;username&gt;\recent-projects.json</c>,
/// but accepts a custom root so tests can point it at a temp directory.
/// </summary>
public sealed class RecentProjectsStore : IRecentProjectsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _rootPath;

    public RecentProjectsStore(string? rootPath = null)
    {
        _rootPath = rootPath ?? GetDefaultRootPath();
    }

    public static string GetDefaultRootPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "BeaverWorks");
    }

    public IReadOnlyList<RecentProjectEntry> LoadRecent(string username)
    {
        var filePath = GetFilePath(username);
        if (!File.Exists(filePath))
        {
            return [];
        }

        var json = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var entries = JsonSerializer.Deserialize<List<RecentProjectEntry>>(json, SerializerOptions);
        return entries ?? [];
    }

    public void RecordOpened(string username, RecentProjectEntry entry)
    {
        var existing = LoadRecent(username)
            .Where(e => !string.Equals(e.FilePath, entry.FilePath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        existing.Insert(0, entry);

        var filePath = GetFilePath(username);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(existing, SerializerOptions);
        File.WriteAllText(filePath, json);
    }

    private string GetFilePath(string username) =>
        Path.Combine(_rootPath, username, "recent-projects.json");
}
