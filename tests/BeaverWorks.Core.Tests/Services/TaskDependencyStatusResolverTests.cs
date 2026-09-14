using BeaverWorks.Core.Models;
using BeaverWorks.Core.Services;

namespace BeaverWorks.Core.Tests.Services;

public class TaskDependencyStatusResolverTests
{
    private static readonly PlanPoint Origin = new() { X = 0.1, Y = 0.1 };

    private static RenovationTask MakeTask(RenovationTaskStatus status = RenovationTaskStatus.Planned)
    {
        var now = DateTimeOffset.UtcNow;
        return new RenovationTask
        {
            Id = Guid.NewGuid(),
            Title = "Task",
            Status = status,
            Position = Origin,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    [Fact]
    public void GetSelectableDependencies_ExcludesSelf()
    {
        var self = MakeTask();
        var other = MakeTask();

        var result = TaskDependencyStatusResolver.GetSelectableDependencies([self, other], excludeTaskId: self.Id);

        Assert.DoesNotContain(self, result);
        Assert.Contains(other, result);
    }

    [Fact]
    public void GetSelectableDependencies_ExcludesDoneTasks()
    {
        var done = MakeTask(RenovationTaskStatus.Done);
        var planned = MakeTask(RenovationTaskStatus.Planned);

        var result = TaskDependencyStatusResolver.GetSelectableDependencies([done, planned], excludeTaskId: null);

        Assert.DoesNotContain(done, result);
        Assert.Contains(planned, result);
    }

    [Fact]
    public void GetSelectableDependencies_NoExcludeId_IncludesAllNonDoneTasks()
    {
        var a = MakeTask();
        var b = MakeTask();

        var result = TaskDependencyStatusResolver.GetSelectableDependencies([a, b], excludeTaskId: null);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ResolveStatus_UnmetDependency_ForcesBlockedRegardlessOfRequestedStatus()
    {
        var dependency = MakeTask(RenovationTaskStatus.Planned);
        var allTasks = new List<RenovationTask> { dependency };

        var result = TaskDependencyStatusResolver.ResolveStatus(RenovationTaskStatus.Done, [dependency.Id], allTasks);

        Assert.Equal(RenovationTaskStatus.Blocked, result);
    }

    [Fact]
    public void ResolveStatus_MissingDependencyId_CountsAsUnmet()
    {
        var missingId = Guid.NewGuid();

        var result = TaskDependencyStatusResolver.ResolveStatus(RenovationTaskStatus.Planned, [missingId], []);

        Assert.Equal(RenovationTaskStatus.Blocked, result);
    }

    [Fact]
    public void ResolveStatus_AllDependenciesDone_ReturnsRequestedStatusUnchanged()
    {
        var dependency = MakeTask(RenovationTaskStatus.Done);
        var allTasks = new List<RenovationTask> { dependency };

        var result = TaskDependencyStatusResolver.ResolveStatus(RenovationTaskStatus.Active, [dependency.Id], allTasks);

        Assert.Equal(RenovationTaskStatus.Active, result);
    }

    [Fact]
    public void ResolveStatus_NoDependencies_ReturnsRequestedStatusUnchanged()
    {
        var result = TaskDependencyStatusResolver.ResolveStatus(RenovationTaskStatus.Planned, [], []);

        Assert.Equal(RenovationTaskStatus.Planned, result);
    }

    [Fact]
    public void ResolveStatusOnEdit_UnmetDependency_ForcesBlocked()
    {
        var dependency = MakeTask(RenovationTaskStatus.Planned);
        var allTasks = new List<RenovationTask> { dependency };

        var result = TaskDependencyStatusResolver.ResolveStatusOnEdit(
            RenovationTaskStatus.Planned, RenovationTaskStatus.Planned, [dependency.Id], allTasks);

        Assert.Equal(RenovationTaskStatus.Blocked, result);
    }

    [Fact]
    public void ResolveStatusOnEdit_PreviouslyBlockedWithNowMetDependency_FallsBackToPlanned()
    {
        var dependency = MakeTask(RenovationTaskStatus.Done);
        var allTasks = new List<RenovationTask> { dependency };

        var result = TaskDependencyStatusResolver.ResolveStatusOnEdit(
            RenovationTaskStatus.Blocked, RenovationTaskStatus.Blocked, [dependency.Id], allTasks);

        Assert.Equal(RenovationTaskStatus.Planned, result);
    }

    [Fact]
    public void ResolveStatusOnEdit_PreviouslyBlockedWithNoDependencies_KeepsExplicitBlockedChoice()
    {
        var result = TaskDependencyStatusResolver.ResolveStatusOnEdit(
            RenovationTaskStatus.Blocked, RenovationTaskStatus.Blocked, [], []);

        Assert.Equal(RenovationTaskStatus.Blocked, result);
    }

    [Fact]
    public void ResolveStatusOnEdit_NotPreviouslyBlocked_ReturnsRequestedStatusUnchanged()
    {
        var dependency = MakeTask(RenovationTaskStatus.Done);
        var allTasks = new List<RenovationTask> { dependency };

        var result = TaskDependencyStatusResolver.ResolveStatusOnEdit(
            RenovationTaskStatus.Blocked, RenovationTaskStatus.Planned, [dependency.Id], allTasks);

        Assert.Equal(RenovationTaskStatus.Blocked, result);
    }
}
