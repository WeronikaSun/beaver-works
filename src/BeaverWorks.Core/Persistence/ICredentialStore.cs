using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Persistence;

/// <summary>
/// Persists the full set of local <see cref="UserCredential"/> records to
/// disk, outside any project file.
/// </summary>
public interface ICredentialStore
{
    IReadOnlyList<UserCredential> LoadAll();

    void SaveAll(IEnumerable<UserCredential> credentials);
}
