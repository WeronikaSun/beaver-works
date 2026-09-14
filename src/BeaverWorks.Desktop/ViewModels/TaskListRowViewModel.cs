using BeaverWorks.Core.Models;

namespace BeaverWorks.Desktop.ViewModels;

/// <summary>
/// One row in the task list panel: the underlying task, plus the
/// recommendation rationale/exclusion label and whether it's currently
/// recommended (FR-014). Immutable — rebuilt wholesale by
/// <see cref="TaskListViewModel.UpdateRecommendations"/> whenever
/// recommendations are recomputed.
/// </summary>
public sealed record TaskListRowViewModel(RenovationTask Task, string Label, bool IsRecommended);
