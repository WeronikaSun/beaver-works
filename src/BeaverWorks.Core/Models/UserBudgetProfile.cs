using BeaverWorks.Core.Services;

namespace BeaverWorks.Core.Models;

/// <summary>
/// One user's declared time/money budgets (FR-013) plus how much has been
/// consumed so far in the current period. Remaining budget is always
/// <em>derived</em> from budget minus consumed rather than stored directly,
/// so editing the declared budget mid-period immediately changes what's
/// left, with no separate "remaining" value to keep in sync (FR-014).
/// </summary>
public sealed class UserBudgetProfile
{
    /// <summary>Declared weekly time budget, in hours (FR-013).</summary>
    public decimal WeeklyTimeBudgetHours { get; set; }

    /// <summary>Declared monthly money budget (FR-013).</summary>
    public decimal MonthlyMoneyBudget { get; set; }

    /// <summary>Hours already consumed (FR-015) within the period identified by <see cref="TimePeriodKey"/>.</summary>
    public decimal TimeConsumedThisWeekHours { get; set; }

    /// <summary>Money already consumed (FR-015) within the period identified by <see cref="MoneyPeriodKey"/>.</summary>
    public decimal MoneyConsumedThisMonth { get; set; }

    /// <summary>The ISO-week key (see <see cref="BudgetPeriodCalculator.GetTimePeriodKey"/>) that <see cref="TimeConsumedThisWeekHours"/> was accumulated against.</summary>
    public required string TimePeriodKey { get; set; }

    /// <summary>The calendar-month key (see <see cref="BudgetPeriodCalculator.GetMoneyPeriodKey"/>) that <see cref="MoneyConsumedThisMonth"/> was accumulated against.</summary>
    public required string MoneyPeriodKey { get; set; }

    /// <summary>Time remaining in the current week, never negative even if consumption exceeds the declared budget.</summary>
    public decimal RemainingTimeHours => Math.Max(0, WeeklyTimeBudgetHours - TimeConsumedThisWeekHours);

    /// <summary>Money remaining in the current month, never negative even if consumption exceeds the declared budget.</summary>
    public decimal RemainingMoney => Math.Max(0, MonthlyMoneyBudget - MoneyConsumedThisMonth);

    /// <summary>
    /// A fresh, zero-budget profile stamped with the current period keys —
    /// used when a user has no budget profile file yet.
    /// </summary>
    public static UserBudgetProfile CreateDefault()
    {
        var now = DateTimeOffset.UtcNow;

        return new UserBudgetProfile
        {
            WeeklyTimeBudgetHours = 0,
            MonthlyMoneyBudget = 0,
            TimeConsumedThisWeekHours = 0,
            MoneyConsumedThisMonth = 0,
            TimePeriodKey = BudgetPeriodCalculator.GetTimePeriodKey(now),
            MoneyPeriodKey = BudgetPeriodCalculator.GetMoneyPeriodKey(now),
        };
    }
}
