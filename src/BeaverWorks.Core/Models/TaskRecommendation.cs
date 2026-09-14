namespace BeaverWorks.Core.Models;

/// <summary>
/// One task's recommendation outcome from <see cref="Services.TaskRecommendationEngine.Recommend"/>:
/// whether it's recommended, plus a short label explaining why (a
/// rationale referencing priority and budget usage when recommended, or an
/// exclusion reason otherwise — FR-014).
/// </summary>
public sealed record TaskRecommendation(Guid TaskId, bool IsRecommended, string Label);
