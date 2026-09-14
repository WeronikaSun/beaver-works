using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Services;

/// <summary>
/// Implements FR-015: deducts a completed task's effective time/cost from
/// the current period's consumption. Uses the same placeholder rule as
/// <see cref="TaskRecommendationEngine"/> for unestimated tasks, so a
/// task's shown rationale and its actual later deduction always agree.
/// </summary>
public static class BudgetConsumptionService
{
    /// <summary>
    /// Adds <paramref name="completedTask"/>'s effective time to
    /// <see cref="UserBudgetProfile.TimeConsumedThisWeekHours"/> and its
    /// effective cost to <see cref="UserBudgetProfile.MoneyConsumedThisMonth"/>,
    /// mutating <paramref name="profile"/> in place. Does not persist the
    /// profile — the caller owns save timing.
    /// </summary>
    public static void ApplyCompletion(UserBudgetProfile profile, RenovationTask completedTask, IReadOnlyList<RenovationTask> allProjectTasks)
    {
        var effectiveTime = completedTask.EstimatedTime is { } time
            ? (decimal)time.TotalHours
            : EstimatePlaceholderCalculator.GetPlaceholderTimeHours(allProjectTasks);

        var effectiveCost = completedTask.EstimatedCost ?? EstimatePlaceholderCalculator.GetPlaceholderCost(allProjectTasks);

        profile.TimeConsumedThisWeekHours += effectiveTime;
        profile.MoneyConsumedThisMonth += effectiveCost;
    }
}
