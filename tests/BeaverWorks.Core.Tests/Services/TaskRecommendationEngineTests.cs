using BeaverWorks.Core.Models;
using BeaverWorks.Core.Services;

namespace BeaverWorks.Core.Tests.Services;

public class TaskRecommendationEngineTests
{
    private static readonly PlanPoint Origin = new() { X = 0.1, Y = 0.1 };

    private static UserBudgetProfile MakeBudget(decimal timeHours, decimal money) => new()
    {
        WeeklyTimeBudgetHours = timeHours,
        MonthlyMoneyBudget = money,
        TimeConsumedThisWeekHours = 0,
        MoneyConsumedThisMonth = 0,
        TimePeriodKey = "2026-W38",
        MoneyPeriodKey = "2026-09",
    };

    private static RenovationTask MakeTask(
        string title,
        int priority,
        RenovationTaskStatus status = RenovationTaskStatus.Planned,
        decimal? cost = null,
        double? timeHours = null,
        DateTimeOffset? createdAt = null,
        List<Guid>? dependsOn = null)
    {
        var now = createdAt ?? DateTimeOffset.UtcNow;
        return new RenovationTask
        {
            Id = Guid.NewGuid(),
            Title = title,
            Status = status,
            Priority = priority,
            EstimatedCost = cost,
            EstimatedTime = timeHours is { } h ? TimeSpan.FromHours(h) : null,
            Position = Origin,
            DependsOnTaskIds = dependsOn ?? [],
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    [Theory]
    [InlineData(RenovationTaskStatus.Done)]
    [InlineData(RenovationTaskStatus.Active)]
    [InlineData(RenovationTaskStatus.Blocked)]
    public void Recommend_TaskInIneligibleStatus_IsExcludedWithStatusLabel(RenovationTaskStatus status)
    {
        var task = MakeTask("Paint wall", priority: 1, status: status, cost: 10, timeHours: 1);
        var budget = MakeBudget(100, 1000);

        var result = TaskRecommendationEngine.Recommend([task], budget);

        var recommendation = Assert.Single(result);
        Assert.False(recommendation.IsRecommended);
        Assert.Equal(status.ToString(), recommendation.Label);
    }

    [Fact]
    public void Recommend_TaskWithUnmetDependency_IsExcludedAsBlockedByDependency()
    {
        var dependency = MakeTask("Buy tiles", priority: 1, status: RenovationTaskStatus.Planned, cost: 5, timeHours: 1);
        var dependent = MakeTask("Lay tiles", priority: 1, cost: 5, timeHours: 1, dependsOn: [dependency.Id]);
        var budget = MakeBudget(100, 1000);

        var result = TaskRecommendationEngine.Recommend([dependency, dependent], budget);

        var dependentResult = result.Single(r => r.TaskId == dependent.Id);
        Assert.False(dependentResult.IsRecommended);
        Assert.Equal("Blocked by dependency", dependentResult.Label);
    }

    [Fact]
    public void Recommend_DependencyCompleted_NoLongerExcludesDependent()
    {
        var dependency = MakeTask("Buy tiles", priority: 1, status: RenovationTaskStatus.Done, cost: 5, timeHours: 1);
        var dependent = MakeTask("Lay tiles", priority: 1, cost: 5, timeHours: 1, dependsOn: [dependency.Id]);
        var budget = MakeBudget(100, 1000);

        var result = TaskRecommendationEngine.Recommend([dependency, dependent], budget);

        var dependentResult = result.Single(r => r.TaskId == dependent.Id);
        Assert.True(dependentResult.IsRecommended);
    }

    [Fact]
    public void Recommend_StatusExclusionTakesPrecedenceOverUnmetDependency()
    {
        var dependency = MakeTask("Buy tiles", priority: 1, status: RenovationTaskStatus.Planned, cost: 5, timeHours: 1);
        var dependent = MakeTask("Lay tiles", priority: 1, status: RenovationTaskStatus.Blocked, cost: 5, timeHours: 1, dependsOn: [dependency.Id]);
        var budget = MakeBudget(100, 1000);

        var result = TaskRecommendationEngine.Recommend([dependency, dependent], budget);

        var dependentResult = result.Single(r => r.TaskId == dependent.Id);
        Assert.Equal("Blocked", dependentResult.Label);
    }

    [Fact]
    public void Recommend_SkipsHigherPriorityTaskThatDoesNotFit_ButPicksUpLowerPriorityTaskThatDoes()
    {
        var expensive = MakeTask("New kitchen", priority: 1, cost: 5000, timeHours: 40);
        var cheap = MakeTask("Fix outlet", priority: 2, cost: 50, timeHours: 2);
        var budget = MakeBudget(10, 200);

        var result = TaskRecommendationEngine.Recommend([expensive, cheap], budget);

        var expensiveResult = result.Single(r => r.TaskId == expensive.Id);
        var cheapResult = result.Single(r => r.TaskId == cheap.Id);

        Assert.False(expensiveResult.IsRecommended);
        Assert.Equal("Over budget", expensiveResult.Label);
        Assert.True(cheapResult.IsRecommended);
    }

    [Fact]
    public void Recommend_EqualPriority_TieBreaksByOlderCreatedAtFirst()
    {
        var older = MakeTask("Older task", priority: 3, cost: 10, timeHours: 1, createdAt: DateTimeOffset.UtcNow.AddDays(-2));
        var newer = MakeTask("Newer task", priority: 3, cost: 10, timeHours: 1, createdAt: DateTimeOffset.UtcNow.AddDays(-1));
        // Budget only fits one of the two tasks; the older one should win the tie-break.
        var budget = MakeBudget(1, 10);

        var result = TaskRecommendationEngine.Recommend([newer, older], budget);

        var olderResult = result.Single(r => r.TaskId == older.Id);
        var newerResult = result.Single(r => r.TaskId == newer.Id);

        Assert.True(olderResult.IsRecommended);
        Assert.False(newerResult.IsRecommended);
    }

    [Fact]
    public void Recommend_UnestimatedTask_UsesPlaceholderFromOtherEstimatedTasks()
    {
        var estimated = MakeTask("Estimated", priority: 2, cost: 100, timeHours: 4);
        var unestimated = MakeTask("Unestimated", priority: 1, cost: null, timeHours: null);
        // Budget fits the placeholder (100/4h, matching the only estimated task) but not both tasks together.
        var budget = MakeBudget(6, 150);

        var result = TaskRecommendationEngine.Recommend([estimated, unestimated], budget);

        var unestimatedResult = result.Single(r => r.TaskId == unestimated.Id);
        Assert.True(unestimatedResult.IsRecommended);
        Assert.Contains("uses 4h", unestimatedResult.Label);
    }

    [Fact]
    public void Recommend_ZeroBudget_ExcludesEveryEligibleTaskAsOverBudget()
    {
        var task = MakeTask("Any task", priority: 1, cost: 1, timeHours: 1);
        var budget = MakeBudget(0, 0);

        var result = TaskRecommendationEngine.Recommend([task], budget);

        var single = Assert.Single(result);
        Assert.False(single.IsRecommended);
        Assert.Equal("Over budget", single.Label);
    }

    [Fact]
    public void Recommend_RecommendedTask_LabelReferencesPriorityAndBudgetUsage()
    {
        var task = MakeTask("Fix outlet", priority: 2, cost: 50, timeHours: 2);
        var budget = MakeBudget(6, 200);

        var result = TaskRecommendationEngine.Recommend([task], budget);

        var single = Assert.Single(result);
        Assert.True(single.IsRecommended);
        Assert.Contains("Priority 2", single.Label);
        Assert.Contains("2h", single.Label);
    }
}
