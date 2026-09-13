using BeaverWorks.Core.Models;
using BeaverWorks.Core.Services;

namespace BeaverWorks.Core.Tests.Services;

public class TaskDependencyValidatorTests
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

    private static Project MakeProject(params RenovationTask[] tasks) => new()
    {
        Name = "Test",
        PlanImagePath = "plan.png",
        CreatedAt = DateTimeOffset.UtcNow,
        Tasks = [.. tasks],
    };

    [Fact]
    public void WouldCreateCycle_SelfReference_ReturnsTrue()
    {
        var taskId = Guid.NewGuid();
        var project = MakeProject(MakeTask(taskId));

        Assert.True(TaskDependencyValidator.WouldCreateCycle(project, taskId, [taskId]));
    }

    [Fact]
    public void WouldCreateCycle_DirectTwoTaskCycle_ReturnsTrue()
    {
        var taskAId = Guid.NewGuid();
        var taskBId = Guid.NewGuid();
        // B already depends on A. Proposing A depends on B would close the cycle.
        var taskA = MakeTask(taskAId);
        var taskB = MakeTask(taskBId, taskAId);
        var project = MakeProject(taskA, taskB);

        Assert.True(TaskDependencyValidator.WouldCreateCycle(project, taskAId, [taskBId]));
    }

    [Fact]
    public void WouldCreateCycle_TransitiveThreeTaskCycle_ReturnsTrue()
    {
        var taskAId = Guid.NewGuid();
        var taskBId = Guid.NewGuid();
        var taskCId = Guid.NewGuid();
        // A -> B -> C already. Proposing C depends on A would close a 3-cycle.
        var taskA = MakeTask(taskAId, taskBId);
        var taskB = MakeTask(taskBId, taskCId);
        var taskC = MakeTask(taskCId);
        var project = MakeProject(taskA, taskB, taskC);

        Assert.True(TaskDependencyValidator.WouldCreateCycle(project, taskCId, [taskAId]));
    }

    [Fact]
    public void WouldCreateCycle_ValidNonCyclicProposal_ReturnsFalse()
    {
        var taskAId = Guid.NewGuid();
        var taskBId = Guid.NewGuid();
        var taskCId = Guid.NewGuid();
        var taskA = MakeTask(taskAId);
        var taskB = MakeTask(taskBId);
        var taskC = MakeTask(taskCId);
        var project = MakeProject(taskA, taskB, taskC);

        // Proposing C depends on A and B, with no existing edges back to C, is not a cycle.
        Assert.False(TaskDependencyValidator.WouldCreateCycle(project, taskCId, [taskAId, taskBId]));
    }
}
