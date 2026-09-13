namespace BeaverWorks.Core.Models;

/// <summary>
/// One project's full contents: its display name, a reference to its plan
/// image, and the renovation tasks pinned to it.
/// </summary>
public sealed class Project
{
    public required string Name { get; set; }

    /// <summary>Absolute path to the plan image file on disk, outside this
    /// project's own JSON — a plain path reference, not embedded bytes.</summary>
    public required string PlanImagePath { get; set; }

    public List<RenovationTask> Tasks { get; set; } = [];

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}
