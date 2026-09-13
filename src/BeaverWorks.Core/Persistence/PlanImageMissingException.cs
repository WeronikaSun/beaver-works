namespace BeaverWorks.Core.Persistence;

/// <summary>
/// Thrown by <see cref="IProjectStore.Load"/> when a project's referenced
/// plan-image file no longer exists on disk. The project's own JSON file is
/// never rewritten or repaired as a result — it is left exactly as-is for
/// the user to fix (e.g. restore the moved image) and retry.
/// </summary>
public sealed class PlanImageMissingException(string planImagePath)
    : Exception($"The plan image referenced by this project was not found: {planImagePath}")
{
    public string PlanImagePath { get; } = planImagePath;
}
