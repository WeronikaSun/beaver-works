using BeaverWorks.Core.Services;

namespace BeaverWorks.Core.Tests.Services;

public class BudgetPeriodCalculatorTests
{
    [Fact]
    public void GetTimePeriodKey_MidWeek_ReturnsIsoYearAndWeek()
    {
        // 2026-09-14 is a Monday in ISO week 38 of 2026.
        var instant = new DateTimeOffset(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

        var key = BudgetPeriodCalculator.GetTimePeriodKey(instant);

        Assert.Equal("2026-W38", key);
    }

    [Fact]
    public void GetTimePeriodKey_LateDecember_UsesIsoWeekYearNotCalendarYear()
    {
        // 2025-12-31 is a Wednesday that falls in ISO week 1 of 2026, even
        // though the calendar year is still 2025.
        var instant = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero);

        var key = BudgetPeriodCalculator.GetTimePeriodKey(instant);

        Assert.Equal("2026-W01", key);
    }

    [Fact]
    public void GetTimePeriodKey_SameIsoWeek_ProducesEqualKeys()
    {
        var monday = new DateTimeOffset(2026, 9, 14, 0, 0, 0, TimeSpan.Zero);
        var sunday = new DateTimeOffset(2026, 9, 20, 23, 59, 0, TimeSpan.Zero);

        Assert.Equal(BudgetPeriodCalculator.GetTimePeriodKey(monday), BudgetPeriodCalculator.GetTimePeriodKey(sunday));
    }

    [Fact]
    public void GetMoneyPeriodKey_ReturnsYearAndMonth()
    {
        var instant = new DateTimeOffset(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

        var key = BudgetPeriodCalculator.GetMoneyPeriodKey(instant);

        Assert.Equal("2026-09", key);
    }

    [Fact]
    public void GetMoneyPeriodKey_DifferentMonths_ProduceDifferentKeys()
    {
        var endOfMonth = new DateTimeOffset(2026, 9, 30, 23, 59, 0, TimeSpan.Zero);
        var startOfNextMonth = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.NotEqual(
            BudgetPeriodCalculator.GetMoneyPeriodKey(endOfMonth),
            BudgetPeriodCalculator.GetMoneyPeriodKey(startOfNextMonth));
    }
}
