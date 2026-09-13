using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Tests.Models;

public class ProjectTests
{
    private static RenovationTask MakeTask(Guid id, params Guid[] dependsOn)
    {
        var position = new PlanPoint { X = 0.1, Y = 0.1 };
        var now = DateTimeOffset.UtcNow;
        return new RenovationTask
        {
            Id = id,
            Title = "Task " + id,
            Position = position,
            CreatedAt = now,
            UpdatedAt = now,
            DependsOnTaskIds = [.. dependsOn],
        };
    }

    [Fact]
    public void GetDependents_ReturnsOnlyTasksThatDependOnGivenTask()
    {
        var targetId = Guid.NewGuid();
        var dependentId = Guid.NewGuid();
        var unrelatedId = Guid.NewGuid();

        var target = MakeTask(targetId);
        var dependent = MakeTask(dependentId, targetId);
        var unrelated = MakeTask(unrelatedId);

        var project = new Project
        {
            Name = "Test",
            PlanImagePath = "plan.png",
            CreatedAt = DateTimeOffset.UtcNow,
            Tasks = [target, dependent, unrelated],
        };

        var dependents = project.GetDependents(targetId);

        var found = Assert.Single(dependents);
        Assert.Equal(dependentId, found.Id);
    }

    [Fact]
    public void GetDependents_TaskDoesNotCountAsItsOwnDependent()
    {
        var taskId = Guid.NewGuid();
        var task = MakeTask(taskId, taskId);

        var project = new Project
        {
            Name = "Test",
            PlanImagePath = "plan.png",
            CreatedAt = DateTimeOffset.UtcNow,
            Tasks = [task],
        };

        Assert.Empty(project.GetDependents(taskId));
    }

    [Fact]
    public void GetDependents_NoTaskDependsOnIt_ReturnsEmpty()
    {
        var targetId = Guid.NewGuid();
        var target = MakeTask(targetId);
        var unrelated = MakeTask(Guid.NewGuid());

        var project = new Project
        {
            Name = "Test",
            PlanImagePath = "plan.png",
            CreatedAt = DateTimeOffset.UtcNow,
            Tasks = [target, unrelated],
        };

        Assert.Empty(project.GetDependents(targetId));
    }
}
