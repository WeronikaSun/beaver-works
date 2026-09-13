namespace BeaverWorks.Core.Models;

/// <summary>
/// One renovation task pinned to a project's plan (FR-007). Only
/// <see cref="Title"/> and <see cref="Position"/> are populated through the
/// current UI; the remaining fields exist with neutral defaults now so the
/// project file format doesn't need to change shape again for S-02/S-03.
/// </summary>
public sealed class RenovationTask
{
    /// <summary>Default priority (1-5 scale) for a newly created task, chosen as a
    /// neutral mid-value so unestimated tasks are neither favored nor penalized.</summary>
    public const int DefaultPriority = 3;

    public required Guid Id { get; init; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public RenovationTaskStatus Status { get; set; } = RenovationTaskStatus.Planned;

    public int Priority { get; set; } = DefaultPriority;

    public decimal? EstimatedCost { get; set; }

    public TimeSpan? EstimatedTime { get; set; }

    public string? RoomId { get; set; }

    public required PlanPoint Position { get; set; }

    public List<Guid> DependsOnTaskIds { get; set; } = [];

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Creates a new task at <paramref name="position"/> with a freshly
    /// generated <see cref="Id"/> and <see cref="CreatedAt"/>/<see cref="UpdatedAt"/>
    /// timestamps set to now. Only <paramref name="title"/> and
    /// <paramref name="position"/> are required; every other field defaults
    /// per this class's declared defaults.
    /// </summary>
    public static RenovationTask Create(
        string title,
        PlanPoint position,
        string? description = null,
        int priority = DefaultPriority,
        decimal? estimatedCost = null,
        TimeSpan? estimatedTime = null,
        string? roomId = null)
    {
        var now = DateTimeOffset.UtcNow;

        return new RenovationTask
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Priority = priority,
            EstimatedCost = estimatedCost,
            EstimatedTime = estimatedTime,
            RoomId = roomId,
            Position = position,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
