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

    /// <summary>
    /// The inverse of <see cref="ApplyCompletion"/>, used when a
    /// previously-<see cref="RenovationTaskStatus.Done"/> task is moved
    /// back to another status: subtracts <paramref name="reactivatedTask"/>'s
    /// effective time/cost from the profile's consumed totals, each floored
    /// at <c>0</c> — mirroring <see cref="UserBudgetProfile.RemainingTimeHours"/>/
    /// <see cref="UserBudgetProfile.RemainingMoney"/>'s existing floor-at-zero
    /// pattern, so reversing consumption can never push remaining budget
    /// above the declared budget. Does not persist the profile — the
    /// caller owns save timing.
    /// </summary>
    public static void ReverseCompletion(UserBudgetProfile profile, RenovationTask reactivatedTask, IReadOnlyList<RenovationTask> allProjectTasks)
    {
        var effectiveTime = reactivatedTask.EstimatedTime is { } time
            ? (decimal)time.TotalHours
            : EstimatePlaceholderCalculator.GetPlaceholderTimeHours(allProjectTasks);

        var effectiveCost = reactivatedTask.EstimatedCost ?? EstimatePlaceholderCalculator.GetPlaceholderCost(allProjectTasks);

        profile.TimeConsumedThisWeekHours = Math.Max(0, profile.TimeConsumedThisWeekHours - effectiveTime);
        profile.MoneyConsumedThisMonth = Math.Max(0, profile.MoneyConsumedThisMonth - effectiveCost);
    }
}
