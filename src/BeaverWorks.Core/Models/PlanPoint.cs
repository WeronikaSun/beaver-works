namespace BeaverWorks.Core.Models;

/// <summary>
/// A task's pinned location on the plan, in normalized (0-1) coordinates
/// relative to the plan image content only. Stored at full <see cref="double"/>
/// precision — no rounding — so a save/load round-trip never drifts.
/// </summary>
public sealed class PlanPoint
{
    public required double X { get; init; }

    public required double Y { get; init; }
}
