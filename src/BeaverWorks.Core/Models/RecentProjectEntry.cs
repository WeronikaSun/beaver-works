namespace BeaverWorks.Core.Models;

/// <summary>
/// One entry in a user's most-recently-used projects list — just enough to
/// display it and reopen it, without loading the full <see cref="Project"/>.
/// </summary>
public sealed class RecentProjectEntry
{
    public required string Name { get; init; }

    public required string FilePath { get; init; }
}
