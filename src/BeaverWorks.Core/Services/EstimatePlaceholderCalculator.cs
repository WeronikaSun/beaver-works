using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Services;

/// <summary>
/// Computes a fair placeholder value for a task's missing
/// <see cref="RenovationTask.EstimatedTime"/>/<see cref="RenovationTask.EstimatedCost"/>,
/// per the PRD's guardrail that an unestimated task must not be silently
/// treated as free (which would unfairly favor it in the recommendation
/// rule). The placeholder is the average of the other tasks' non-null
/// estimates in the same set, recomputed fresh each call rather than
/// cached, so it always reflects the current project.
/// </summary>
public static class EstimatePlaceholderCalculator
{
    /// <summary>
    /// The average estimated time (in hours) across every task in
    /// <paramref name="tasks"/> that has a non-null
    /// <see cref="RenovationTask.EstimatedTime"/>, or <c>0</c> if none do.
    /// </summary>
    public static decimal GetPlaceholderTimeHours(IEnumerable<RenovationTask> tasks)
    {
        var estimatedHours = tasks
            .Where(t => t.EstimatedTime is not null)
            .Select(t => (decimal)t.EstimatedTime!.Value.TotalHours)
            .ToList();

        return estimatedHours.Count == 0 ? 0 : estimatedHours.Average();
    }

    /// <summary>
    /// The average estimated cost across every task in
    /// <paramref name="tasks"/> that has a non-null
    /// <see cref="RenovationTask.EstimatedCost"/>, or <c>0</c> if none do.
    /// </summary>
    public static decimal GetPlaceholderCost(IEnumerable<RenovationTask> tasks)
    {
        var estimatedCosts = tasks
            .Where(t => t.EstimatedCost is not null)
            .Select(t => t.EstimatedCost!.Value)
            .ToList();

        return estimatedCosts.Count == 0 ? 0 : estimatedCosts.Average();
    }
}
