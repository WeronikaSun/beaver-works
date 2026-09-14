using BeaverWorks.Core.Services;

namespace BeaverWorks.Core.Tests.Services;

public class TaskFieldValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateTitle_EmptyOrWhitespace_ReturnsError(string title)
    {
        Assert.NotNull(TaskFieldValidator.ValidateTitle(title));
    }

    [Fact]
    public void ValidateTitle_NonEmpty_ReturnsNull()
    {
        Assert.Null(TaskFieldValidator.ValidateTitle("Paint wall"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void ValidatePriority_OutOfRange_ReturnsError(int priority)
    {
        Assert.NotNull(TaskFieldValidator.ValidatePriority(priority));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void ValidatePriority_InRange_ReturnsNull(int priority)
    {
        Assert.Null(TaskFieldValidator.ValidatePriority(priority));
    }

    [Fact]
    public void ValidateEstimatedCost_Negative_ReturnsError()
    {
        Assert.NotNull(TaskFieldValidator.ValidateEstimatedCost(-1m));
    }

    [Fact]
    public void ValidateEstimatedCost_Null_ReturnsNull()
    {
        Assert.Null(TaskFieldValidator.ValidateEstimatedCost(null));
    }

    [Fact]
    public void ValidateEstimatedCost_ZeroOrPositive_ReturnsNull()
    {
        Assert.Null(TaskFieldValidator.ValidateEstimatedCost(0m));
        Assert.Null(TaskFieldValidator.ValidateEstimatedCost(100m));
    }

    [Fact]
    public void TryResolveEstimatedTime_Negative_FailsWithError()
    {
        var succeeded = TaskFieldValidator.TryResolveEstimatedTime(-1m, out var time, out var error);

        Assert.False(succeeded);
        Assert.Null(time);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryResolveEstimatedTime_ExceedsMax_FailsWithError()
    {
        var succeeded = TaskFieldValidator.TryResolveEstimatedTime(TaskFieldValidator.MaxEstimatedTimeHours + 1, out var time, out var error);

        Assert.False(succeeded);
        Assert.Null(time);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryResolveEstimatedTime_Null_SucceedsWithNullTime()
    {
        var succeeded = TaskFieldValidator.TryResolveEstimatedTime(null, out var time, out var error);

        Assert.True(succeeded);
        Assert.Null(time);
        Assert.Null(error);
    }

    [Fact]
    public void TryResolveEstimatedTime_ValidHours_SucceedsWithConvertedTimeSpan()
    {
        var succeeded = TaskFieldValidator.TryResolveEstimatedTime(2.5m, out var time, out var error);

        Assert.True(succeeded);
        Assert.Equal(TimeSpan.FromHours(2.5), time);
        Assert.Null(error);
    }
}
