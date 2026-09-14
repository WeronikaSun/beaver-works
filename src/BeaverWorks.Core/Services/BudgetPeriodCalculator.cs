using System.Globalization;

namespace BeaverWorks.Core.Services;

/// <summary>
/// Computes the stable period-key strings used to detect when a
/// <see cref="Models.UserBudgetProfile"/>'s time (weekly) or money
/// (monthly) budget has rolled over into a new calendar period (FR-016).
/// Both methods are pure functions of the given instant, evaluated in UTC
/// so results don't depend on the caller's local time zone.
/// </summary>
public static class BudgetPeriodCalculator
{
    /// <summary>
    /// The ISO-8601 week (Monday-start) containing <paramref name="instant"/>,
    /// e.g. <c>"2026-W38"</c>. Note the returned year is the ISO week-year,
    /// which can differ from the calendar year for dates near year
    /// boundaries (e.g. late December can fall in week 1 of the next ISO
    /// year).
    /// </summary>
    public static string GetTimePeriodKey(DateTimeOffset instant)
    {
        var date = instant.UtcDateTime;
        var isoYear = ISOWeek.GetYear(date);
        var isoWeek = ISOWeek.GetWeekOfYear(date);
        return $"{isoYear}-W{isoWeek:D2}";
    }

    /// <summary>
    /// The calendar month containing <paramref name="instant"/>, e.g.
    /// <c>"2026-09"</c>.
    /// </summary>
    public static string GetMoneyPeriodKey(DateTimeOffset instant) =>
        instant.UtcDateTime.ToString("yyyy-MM", CultureInfo.InvariantCulture);
}
