using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Services;

/// <summary>
/// The single definition of "unmet dependency", shared between the
/// recommendation engine and the Create/Edit task dialogs: a dependency is
/// unmet when the task it points to either no longer exists in the project
/// or hasn't reached <see cref="RenovationTaskStatus.Done"/>. Also decides
/// which tasks are selectable as a dependency in the first place — a
/// <see cref="RenovationTaskStatus.Done"/> task can't meaningfully "block"
/// anything, so it's excluded from the picker rather than offered and then
/// treated as always-met.
/// </summary>
public static class TaskDependencyStatusResolver
{
    /// <summary>
    /// Every task in <paramref name="allProjectTasks"/> that could be
    /// selected as a dependency: excludes <paramref name="excludeTaskId"/>
    /// (the task being edited, if any — pass <see langword="null"/> when
    /// creating a task that doesn't exist yet) and any task whose
    /// <see cref="RenovationTask.Status"/> is <see cref="RenovationTaskStatus.Done"/>.
    /// </summary>
    public static IReadOnlyList<RenovationTask> GetSelectableDependencies(
        IEnumerable<RenovationTask> allProjectTasks,
        Guid? excludeTaskId) =>
        allProjectTasks
            .Where(t => t.Id != excludeTaskId && t.Status != RenovationTaskStatus.Done)
            .ToList();

    /// <summary>
    /// Returns <see cref="RenovationTaskStatus.Blocked"/> if any id in
    /// <paramref name="dependsOnIds"/> is unmet (per <see cref="HasUnmetDependency"/>);
    /// otherwise returns <paramref name="requestedStatus"/> unchanged. This
    /// overrides any explicit status choice — a task can't be saved as
    /// e.g. <see cref="RenovationTaskStatus.Done"/> or
    /// <see cref="RenovationTaskStatus.Active"/> while it still has an
    /// unmet dependency.
    /// </summary>
    public static RenovationTaskStatus ResolveStatus(
        RenovationTaskStatus requestedStatus,
        IReadOnlyList<Guid> dependsOnIds,
        IReadOnlyList<RenovationTask> allProjectTasks)
    {
        var tasksById = allProjectTasks.ToDictionary(t => t.Id);
        return HasUnmetDependency(dependsOnIds, tasksById) ? RenovationTaskStatus.Blocked : requestedStatus;
    }

    /// <summary>
    /// A dependency is "unmet" when the task it points to either no longer
    /// exists in <paramref name="tasksById"/> or hasn't reached
    /// <see cref="RenovationTaskStatus.Done"/>.
    /// </summary>
    public static bool HasUnmetDependency(IEnumerable<Guid> dependsOnIds, IReadOnlyDictionary<Guid, RenovationTask> tasksById) =>
        dependsOnIds.Any(id => !tasksById.TryGetValue(id, out var dependency) || dependency.Status != RenovationTaskStatus.Done);
}
