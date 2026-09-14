using BeaverWorks.Core.Models;
using BeaverWorks.Core.Services;

namespace BeaverWorks.Core.Tests.Services;

public class EstimatePlaceholderCalculatorTests
{
    private static RenovationTask MakeTask(decimal? estimatedCost, double? estimatedTimeHours) =>
        RenovationTask.Create(
            title: "Task",
            position: new PlanPoint { X = 0.5, Y = 0.5 },
            estimatedCost: estimatedCost,
            estimatedTime: estimatedTimeHours is { } hours ? TimeSpan.FromHours(hours) : null);

    [Fact]
    public void GetPlaceholderTimeHours_MixedNullAndEstimatedTasks_AveragesOnlyEstimatedOnes()
    {
        var tasks = new[]
        {
            MakeTask(estimatedCost: null, estimatedTimeHours: 2),
            MakeTask(estimatedCost: null, estimatedTimeHours: 4),
            MakeTask(estimatedCost: null, estimatedTimeHours: null),
        };

        var placeholder = EstimatePlaceholderCalculator.GetPlaceholderTimeHours(tasks);

        Assert.Equal(3m, placeholder);
    }

    [Fact]
    public void GetPlaceholderTimeHours_NoTaskHasEstimate_ReturnsZero()
    {
        var tasks = new[]
        {
            MakeTask(estimatedCost: null, estimatedTimeHours: null),
            MakeTask(estimatedCost: null, estimatedTimeHours: null),
        };

        var placeholder = EstimatePlaceholderCalculator.GetPlaceholderTimeHours(tasks);

        Assert.Equal(0m, placeholder);
    }

    [Fact]
    public void GetPlaceholderCost_MixedNullAndEstimatedTasks_AveragesOnlyEstimatedOnes()
    {
        var tasks = new[]
        {
            MakeTask(estimatedCost: 100m, estimatedTimeHours: null),
            MakeTask(estimatedCost: 300m, estimatedTimeHours: null),
            MakeTask(estimatedCost: null, estimatedTimeHours: null),
        };

        var placeholder = EstimatePlaceholderCalculator.GetPlaceholderCost(tasks);

        Assert.Equal(200m, placeholder);
    }

    [Fact]
    public void GetPlaceholderCost_NoTaskHasEstimate_ReturnsZero()
    {
        var tasks = new[]
        {
            MakeTask(estimatedCost: null, estimatedTimeHours: null),
        };

        var placeholder = EstimatePlaceholderCalculator.GetPlaceholderCost(tasks);

        Assert.Equal(0m, placeholder);
    }
}
