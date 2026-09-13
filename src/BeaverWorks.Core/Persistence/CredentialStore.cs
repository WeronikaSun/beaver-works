using System.Text.Json;
using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Persistence;

/// <summary>
/// JSON-file-backed <see cref="ICredentialStore"/>. Defaults to
/// %LOCALAPPDATA%\BeaverWorks\credentials.json, a location outside any
/// project file, but accepts a custom path so tests can point it elsewhere.
/// </summary>
public sealed class CredentialStore : ICredentialStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;

    public CredentialStore(string? filePath = null)
    {
        _filePath = filePath ?? GetDefaultFilePath();
    }

    public static string GetDefaultFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "BeaverWorks", "credentials.json");
    }

    public IReadOnlyList<UserCredential> LoadAll()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var credentials = JsonSerializer.Deserialize<List<UserCredential>>(json, SerializerOptions);
        return credentials ?? [];
    }

    public void SaveAll(IEnumerable<UserCredential> credentials)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(credentials.ToList(), SerializerOptions);
        File.WriteAllText(_filePath, json);
    }
}
