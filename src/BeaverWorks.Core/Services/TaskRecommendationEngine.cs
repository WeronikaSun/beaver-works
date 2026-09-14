using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Services;

/// <summary>
/// Implements FR-014: given a project's tasks and the user's budget
/// profile, decides which tasks are recommended (fit within the remaining
/// time/money budget, in priority order) and which are excluded, with a
/// short label explaining either outcome.
/// </summary>
public static class TaskRecommendationEngine
{
    /// <summary>
    /// Computes one <see cref="TaskRecommendation"/> per task in
    /// <paramref name="tasks"/>, in the same order as the input.
    /// </summary>
    /// <remarks>
    /// Exclusion precedence (checked in this order): a task's own status
    /// (Done/Active/Blocked) &gt; an unmet dependency &gt; not fitting the
    /// remaining budget. Eligible candidates are sorted by
    /// <see cref="RenovationTask.Priority"/> ascending (1 = highest), then
    /// <see cref="RenovationTask.CreatedAt"/> ascending as a tie-break, and
    /// walked once: a candidate that fits the running remaining time
    /// <em>and</em> money is recommended and subtracted from the running
    /// remainder; a candidate that doesn't fit is skipped (not stopped on)
    /// so a later, lower-priority task that does fit is still picked up.
    /// Effective time/cost for a task falls back to
    /// <see cref="EstimatePlaceholderCalculator"/> when unestimated.
    /// </remarks>
    public static IReadOnlyList<TaskRecommendation> Recommend(IReadOnlyList<RenovationTask> tasks, UserBudgetProfile budget)
    {
        var placeholderTimeHours = EstimatePlaceholderCalculator.GetPlaceholderTimeHours(tasks);
        var placeholderCost = EstimatePlaceholderCalculator.GetPlaceholderCost(tasks);
        var tasksById = tasks.ToDictionary(t => t.Id);

        var labelsByTaskId = new Dictionary<Guid, string>();
        var recommendedIds = new HashSet<Guid>();

        var candidates = new List<RenovationTask>();
        foreach (var task in tasks)
        {
            if (task.Status is RenovationTaskStatus.Done or RenovationTaskStatus.Active or RenovationTaskStatus.Blocked)
            {
                labelsByTaskId[task.Id] = task.Status.ToString();
                continue;
            }

            if (HasUnmetDependency(task, tasksById))
            {
                labelsByTaskId[task.Id] = "Blocked by dependency";
                continue;
            }

            candidates.Add(task);
        }

        var startingRemainingTime = budget.RemainingTimeHours;
        var startingRemainingMoney = budget.RemainingMoney;
        var runningRemainingTime = startingRemainingTime;
        var runningRemainingMoney = startingRemainingMoney;

        foreach (var candidate in candidates
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.CreatedAt))
        {
            var effectiveTime = candidate.EstimatedTime is { } time ? (decimal)time.TotalHours : placeholderTimeHours;
            var effectiveCost = candidate.EstimatedCost ?? placeholderCost;

            if (effectiveTime <= runningRemainingTime && effectiveCost <= runningRemainingMoney)
            {
                runningRemainingTime -= effectiveTime;
                runningRemainingMoney -= effectiveCost;
                recommendedIds.Add(candidate.Id);
                labelsByTaskId[candidate.Id] = FormatRationale(candidate, effectiveTime, effectiveCost, startingRemainingTime, startingRemainingMoney);
            }
            else
            {
                labelsByTaskId[candidate.Id] = "Over budget";
            }
        }

        return tasks
            .Select(t => new TaskRecommendation(t.Id, recommendedIds.Contains(t.Id), labelsByTaskId[t.Id]))
            .ToList();
    }

    /// <summary>
    /// A dependency is "unmet" when the task it points to either no longer
    /// exists in the project or hasn't reached <see cref="RenovationTaskStatus.Done"/>.
    /// Delegates to <see cref="TaskDependencyStatusResolver"/> so this and
    /// the Create/Edit task dialogs share one definition.
    /// </summary>
    private static bool HasUnmetDependency(RenovationTask task, IReadOnlyDictionary<Guid, RenovationTask> tasksById) =>
        TaskDependencyStatusResolver.HasUnmetDependency(task.DependsOnTaskIds, tasksById);

    private static string FormatRationale(RenovationTask task, decimal effectiveTime, decimal effectiveCost, decimal startingRemainingTime, decimal startingRemainingMoney) =>
        // Matches TaskListViewModel's existing "cost.ToString(\"C\")" convention
        // (current-culture currency formatting) rather than a fixed culture.
        $"Priority {task.Priority} · uses {effectiveTime:0.#}h of {startingRemainingTime:0.#}h, {effectiveCost:C} of {startingRemainingMoney:C}";
}
