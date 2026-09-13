using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Persistence;

/// <summary>
/// Persists a per-user, most-recent-first list of projects that have been
/// opened or saved, so the recent-projects screen has something real to
/// read.
/// </summary>
public interface IRecentProjectsStore
{
    IReadOnlyList<RecentProjectEntry> LoadRecent(string username);

    /// <summary>
    /// Records that <paramref name="entry"/> was just opened/saved for
    /// <paramref name="username"/>: inserts it at the front of that user's
    /// list, moving it there (rather than duplicating it) if an entry with
    /// the same <see cref="RecentProjectEntry.FilePath"/> already exists.
    /// </summary>
    void RecordOpened(string username, RecentProjectEntry entry);
}
