using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Tests.Models;

public class RenovationTaskTests
{
    [Fact]
    public void Create_NewTask_DefaultsToPlannedStatusAndMidPriorityAndEmptyDependencies()
    {
        var position = new PlanPoint { X = 0.5, Y = 0.5 };

        var task = RenovationTask.Create("Replace outlet", position);

        Assert.Equal(RenovationTaskStatus.Planned, task.Status);
        Assert.InRange(task.Priority, 1, 5);
        Assert.Empty(task.DependsOnTaskIds);
        Assert.Equal(position.X, task.Position.X);
        Assert.Equal(position.Y, task.Position.Y);
        Assert.NotEqual(Guid.Empty, task.Id);
    }

    [Fact]
    public void PriorityBounds_MinAndMaxMatchDeclaredOneToFiveScale()
    {
        Assert.Equal(1, RenovationTask.MinPriority);
        Assert.Equal(5, RenovationTask.MaxPriority);
        Assert.InRange(RenovationTask.DefaultPriority, RenovationTask.MinPriority, RenovationTask.MaxPriority);
    }
}
