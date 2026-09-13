namespace BeaverWorks.Core.Models;

/// <summary>
/// Lifecycle state of a <see cref="RenovationTask"/>, driving the marker
/// color shown on the plan (FR-008). Only <see cref="Planned"/> is
/// reachable through the current UI; the others exist so S-02/S-03 don't
/// require a breaking model change.
/// </summary>
/// <remarks>
/// Named <c>RenovationTaskStatus</c> rather than <c>TaskStatus</c> to avoid
/// colliding with <see cref="System.Threading.Tasks.TaskStatus"/>, which is
/// implicitly in scope via <c>ImplicitUsings</c> in every consuming file.
/// </remarks>
public enum RenovationTaskStatus
{
    Planned,
    Active,
    Blocked,
    Done,
}
