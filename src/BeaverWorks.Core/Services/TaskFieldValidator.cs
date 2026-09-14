using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Services;

/// <summary>
/// Field-level validation shared between the Create and Edit task dialogs,
/// so the two don't drift apart as they gain field parity. Every method
/// returns <see langword="null"/> when the input is valid, or a
/// user-facing error message when it isn't.
/// </summary>
public static class TaskFieldValidator
{
    /// <summary>
    /// Upper bound for an estimated-time input (in hours) so a very large
    /// value can't overflow <see cref="TimeSpan"/> when converted on save.
    /// ~100,000 hours (over 11 years) comfortably covers any real
    /// renovation estimate while staying well under <see cref="TimeSpan.MaxValue"/>.
    /// </summary>
    public const decimal MaxEstimatedTimeHours = 100_000m;

    public static string? ValidateTitle(string title) =>
        title.Trim().Length == 0 ? "Please enter a title." : null;

    public static string? ValidatePriority(int priority) =>
        priority < RenovationTask.MinPriority || priority > RenovationTask.MaxPriority
            ? $"Priority must be between {RenovationTask.MinPriority} and {RenovationTask.MaxPriority}."
            : null;

    public static string? ValidateEstimatedCost(decimal? cost) =>
        cost is < 0 ? "Estimated cost cannot be negative." : null;

    /// <summary>
    /// Validates <paramref name="hours"/> and, when valid, converts it to a
    /// <see cref="TimeSpan"/> via <paramref name="time"/>. Returns a
    /// user-facing error message via <paramref name="error"/> instead when
    /// the value is negative, too large, or otherwise out of range.
    /// </summary>
    public static bool TryResolveEstimatedTime(decimal? hours, out TimeSpan? time, out string? error)
    {
        time = null;

        if (hours is < 0)
        {
            error = "Estimated time cannot be negative.";
            return false;
        }

        if (hours > MaxEstimatedTimeHours)
        {
            error = $"Estimated time is too large (max {MaxEstimatedTimeHours:N0} hours).";
            return false;
        }

        try
        {
            time = hours is { } h ? TimeSpan.FromHours((double)h) : null;
        }
        catch (OverflowException)
        {
            error = "Estimated time is out of range.";
            return false;
        }

        error = null;
        return true;
    }
}
