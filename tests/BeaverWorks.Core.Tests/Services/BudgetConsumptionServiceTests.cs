using BeaverWorks.Core.Models;
using BeaverWorks.Core.Services;

namespace BeaverWorks.Core.Tests.Services;

public class BudgetConsumptionServiceTests
{
    private static readonly PlanPoint Origin = new() { X = 0.2, Y = 0.2 };

    private static UserBudgetProfile MakeBudget() => new()
    {
        WeeklyTimeBudgetHours = 20,
        MonthlyMoneyBudget = 1000,
        TimeConsumedThisWeekHours = 0,
        MoneyConsumedThisMonth = 0,
        TimePeriodKey = "2026-W38",
        MoneyPeriodKey = "2026-09",
    };

    private static RenovationTask MakeTask(decimal? cost, double? timeHours)
    {
        var now = DateTimeOffset.UtcNow;
        return new RenovationTask
        {
            Id = Guid.NewGuid(),
            Title = "Task",
            Status = RenovationTaskStatus.Done,
            EstimatedCost = cost,
            EstimatedTime = timeHours is { } h ? TimeSpan.FromHours(h) : null,
            Position = Origin,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    [Fact]
    public void ApplyCompletion_EstimatedTask_DeductsItsOwnEstimate()
    {
        var profile = MakeBudget();
        var task = MakeTask(cost: 150m, timeHours: 3);

        BudgetConsumptionService.ApplyCompletion(profile, task, [task]);

        Assert.Equal(3, profile.TimeConsumedThisWeekHours);
        Assert.Equal(150, profile.MoneyConsumedThisMonth);
    }

    [Fact]
    public void ApplyCompletion_UnestimatedTask_DeductsPlaceholderFromOtherProjectTasks()
    {
        var profile = MakeBudget();
        var estimatedSibling = MakeTask(cost: 200m, timeHours: 8);
        var unestimatedTask = MakeTask(cost: null, timeHours: null);

        BudgetConsumptionService.ApplyCompletion(profile, unestimatedTask, [estimatedSibling, unestimatedTask]);

        Assert.Equal(8, profile.TimeConsumedThisWeekHours);
        Assert.Equal(200, profile.MoneyConsumedThisMonth);
    }

    [Fact]
    public void ApplyCompletion_CalledTwice_AccumulatesIndependentlyAcrossTimeAndMoney()
    {
        var profile = MakeBudget();
        var first = MakeTask(cost: 50m, timeHours: 2);
        var second = MakeTask(cost: 30m, timeHours: 1);

        BudgetConsumptionService.ApplyCompletion(profile, first, [first, second]);
        BudgetConsumptionService.ApplyCompletion(profile, second, [first, second]);

        Assert.Equal(3, profile.TimeConsumedThisWeekHours);
        Assert.Equal(80, profile.MoneyConsumedThisMonth);
    }

    [Fact]
    public void ReverseCompletion_EstimatedTask_SubtractsItsOwnEstimate()
    {
        var profile = MakeBudget();
        var task = MakeTask(cost: 150m, timeHours: 3);
        BudgetConsumptionService.ApplyCompletion(profile, task, [task]);

        BudgetConsumptionService.ReverseCompletion(profile, task, [task]);

        Assert.Equal(0, profile.TimeConsumedThisWeekHours);
        Assert.Equal(0, profile.MoneyConsumedThisMonth);
    }

    [Fact]
    public void ReverseCompletion_UnestimatedTask_SubtractsPlaceholderFromOtherProjectTasks()
    {
        var profile = MakeBudget();
        var estimatedSibling = MakeTask(cost: 200m, timeHours: 8);
        var unestimatedTask = MakeTask(cost: null, timeHours: null);
        BudgetConsumptionService.ApplyCompletion(profile, unestimatedTask, [estimatedSibling, unestimatedTask]);

        BudgetConsumptionService.ReverseCompletion(profile, unestimatedTask, [estimatedSibling, unestimatedTask]);

        Assert.Equal(0, profile.TimeConsumedThisWeekHours);
        Assert.Equal(0, profile.MoneyConsumedThisMonth);
    }

    [Fact]
    public void ReverseCompletion_WouldGoNegative_FloorsConsumedAtZeroInsteadOfExceedingDeclaredBudget()
    {
        var profile = MakeBudget();
        profile.TimeConsumedThisWeekHours = 1;
        profile.MoneyConsumedThisMonth = 10;
        var task = MakeTask(cost: 150m, timeHours: 3);

        BudgetConsumptionService.ReverseCompletion(profile, task, [task]);

        Assert.Equal(0, profile.TimeConsumedThisWeekHours);
        Assert.Equal(0, profile.MoneyConsumedThisMonth);
        // Remaining budget never exceeds the declared budget even though the reversal "overshot".
        Assert.Equal(profile.WeeklyTimeBudgetHours, profile.RemainingTimeHours);
        Assert.Equal(profile.MonthlyMoneyBudget, profile.RemainingMoney);
    }

    [Fact]
    public void ReverseCompletion_IsInverseOfApplyCompletion_WhenBudgetIsNotExceeded()
    {
        var profile = MakeBudget();
        profile.TimeConsumedThisWeekHours = 5;
        profile.MoneyConsumedThisMonth = 500;
        var task = MakeTask(cost: 150m, timeHours: 3);

        BudgetConsumptionService.ApplyCompletion(profile, task, [task]);
        BudgetConsumptionService.ReverseCompletion(profile, task, [task]);

        Assert.Equal(5, profile.TimeConsumedThisWeekHours);
        Assert.Equal(500, profile.MoneyConsumedThisMonth);
    }
}
