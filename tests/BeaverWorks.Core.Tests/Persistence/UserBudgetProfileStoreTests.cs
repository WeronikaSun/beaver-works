using BeaverWorks.Core.Models;
using BeaverWorks.Core.Persistence;
using BeaverWorks.Core.Services;

namespace BeaverWorks.Core.Tests.Persistence;

public class UserBudgetProfileStoreTests : IDisposable
{
    private readonly string _tempRoot;

    public UserBudgetProfileStoreTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"beaverworks-budget-profile-tests-{Guid.NewGuid()}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Load_NoFileYet_ReturnsZeroBudgetDefaultRatherThanThrowing()
    {
        var store = new UserBudgetProfileStore(_tempRoot);

        var profile = store.Load("anna");

        Assert.Equal(0, profile.WeeklyTimeBudgetHours);
        Assert.Equal(0, profile.MonthlyMoneyBudget);
        Assert.Equal(0, profile.RemainingTimeHours);
        Assert.Equal(0, profile.RemainingMoney);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsDeclaredBudgetsAndConsumption()
    {
        var store = new UserBudgetProfileStore(_tempRoot);
        var now = DateTimeOffset.UtcNow;
        var profile = new UserBudgetProfile
        {
            WeeklyTimeBudgetHours = 10,
            MonthlyMoneyBudget = 500,
            TimeConsumedThisWeekHours = 3,
            MoneyConsumedThisMonth = 120,
            TimePeriodKey = BudgetPeriodCalculator.GetTimePeriodKey(now),
            MoneyPeriodKey = BudgetPeriodCalculator.GetMoneyPeriodKey(now),
        };

        store.Save("anna", profile);
        var reloaded = store.Load("anna");

        Assert.Equal(10, reloaded.WeeklyTimeBudgetHours);
        Assert.Equal(500, reloaded.MonthlyMoneyBudget);
        Assert.Equal(3, reloaded.TimeConsumedThisWeekHours);
        Assert.Equal(120, reloaded.MoneyConsumedThisMonth);
        Assert.Equal(7, reloaded.RemainingTimeHours);
        Assert.Equal(380, reloaded.RemainingMoney);
    }

    [Fact]
    public void Load_StaleTimePeriodKey_ResetsOnlyTimeConsumption()
    {
        var store = new UserBudgetProfileStore(_tempRoot);
        var now = DateTimeOffset.UtcNow;
        var profile = new UserBudgetProfile
        {
            WeeklyTimeBudgetHours = 10,
            MonthlyMoneyBudget = 500,
            TimeConsumedThisWeekHours = 8,
            MoneyConsumedThisMonth = 120,
            TimePeriodKey = "stale-week-key",
            MoneyPeriodKey = BudgetPeriodCalculator.GetMoneyPeriodKey(now),
        };
        store.Save("anna", profile);

        var reloaded = store.Load("anna");

        Assert.Equal(0, reloaded.TimeConsumedThisWeekHours);
        Assert.Equal(BudgetPeriodCalculator.GetTimePeriodKey(now), reloaded.TimePeriodKey);
        Assert.Equal(120, reloaded.MoneyConsumedThisMonth);
    }

    [Fact]
    public void Load_StaleMoneyPeriodKey_ResetsOnlyMoneyConsumption()
    {
        var store = new UserBudgetProfileStore(_tempRoot);
        var now = DateTimeOffset.UtcNow;
        var profile = new UserBudgetProfile
        {
            WeeklyTimeBudgetHours = 10,
            MonthlyMoneyBudget = 500,
            TimeConsumedThisWeekHours = 3,
            MoneyConsumedThisMonth = 450,
            TimePeriodKey = BudgetPeriodCalculator.GetTimePeriodKey(now),
            MoneyPeriodKey = "stale-month-key",
        };
        store.Save("anna", profile);

        var reloaded = store.Load("anna");

        Assert.Equal(0, reloaded.MoneyConsumedThisMonth);
        Assert.Equal(BudgetPeriodCalculator.GetMoneyPeriodKey(now), reloaded.MoneyPeriodKey);
        Assert.Equal(3, reloaded.TimeConsumedThisWeekHours);
    }

    [Fact]
    public void Load_StalePeriodKey_PersistsResetSoItSurvivesASecondLoad()
    {
        var store = new UserBudgetProfileStore(_tempRoot);
        var profile = new UserBudgetProfile
        {
            WeeklyTimeBudgetHours = 10,
            MonthlyMoneyBudget = 500,
            TimeConsumedThisWeekHours = 8,
            MoneyConsumedThisMonth = 120,
            TimePeriodKey = "stale-week-key",
            MoneyPeriodKey = "stale-month-key",
        };
        store.Save("anna", profile);

        store.Load("anna");
        var secondLoad = store.Load("anna");

        Assert.Equal(0, secondLoad.TimeConsumedThisWeekHours);
        Assert.Equal(0, secondLoad.MoneyConsumedThisMonth);
    }
}
