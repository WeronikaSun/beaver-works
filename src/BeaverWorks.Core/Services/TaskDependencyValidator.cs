using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Services;

/// <summary>
/// Pure dependency-graph check used before a task's
/// <see cref="RenovationTask.DependsOnTaskIds"/> edit is saved, so a cycle
/// (e.g. A depends on B depends on A) can never be persisted. A cycle would
/// make S-03's "must be completed" dependency gating impossible to satisfy
/// for every task on the cycle.
/// </summary>
public static class TaskDependencyValidator
{
    /// <summary>
    /// Returns <see langword="true"/> if adopting <paramref name="proposedDependsOnIds"/>
    /// as <paramref name="taskId"/>'s dependency set would create a cycle —
    /// either a direct self-reference, or a path that walks through other
    /// tasks' existing <see cref="RenovationTask.DependsOnTaskIds"/> edges in
    /// <paramref name="project"/> and eventually reaches <paramref name="taskId"/>
    /// again.
    /// </summary>
    public static bool WouldCreateCycle(Project project, Guid taskId, IEnumerable<Guid> proposedDependsOnIds)
    {
        var tasksById = project.Tasks.ToDictionary(t => t.Id);
        var visited = new HashSet<Guid>();
        var stack = new Stack<Guid>(proposedDependsOnIds);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current == taskId)
            {
                return true;
            }

            if (!visited.Add(current))
            {
                continue;
            }

            if (tasksById.TryGetValue(current, out var currentTask))
            {
                foreach (var next in currentTask.DependsOnTaskIds)
                {
                    stack.Push(next);
                }
            }
        }

        return false;
    }
}
