<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Manage Tasks Implementation Plan

- **Plan**: context/changes/manage-tasks/plan.md
- **Scope**: Phase 3 of 3 (full plan)
- **Date**: 2026-09-14
- **Verdict**: NEEDS ATTENTION
- **Findings**: 0 critical, 5 warnings, 1 observation

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | WARNING |
| Scope Discipline | PASS |
| Safety & Quality | WARNING |
| Architecture | PASS |
| Pattern Consistency | PASS |
| Success Criteria | PASS |

## Findings

### F1 — Task list rows don't show a status-color swatch

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Adherence
- **Location**: src/BeaverWorks.Desktop/Views/TaskListView.xaml:20-24
- **Detail**: Phase 2's contract for the task list view said each row should show "a small colored swatch via `TaskStatusColors.ForStatus` + `Title`". The `ListBox` currently uses `DisplayMemberPath="Title"` only — no swatch. Functionally harmless (status is still visible via the detail panel and the canvas marker color), but the plan's stated UI contract for the list itself isn't fully met.
- **Fix**: Replace `DisplayMemberPath="Title"` with an `ItemTemplate` containing a small colored `Ellipse`/`Rectangle` (bound via a `RenovationTaskStatus`→`Brush` converter using `TaskStatusColors.ForStatus`) followed by a `TextBlock` bound to `Title`.
- **Decision**: FIXED — added `TaskStatusToBrushConverter.cs` (reuses `TaskStatusColors.ForStatus`) and an `ItemTemplate` with a status-color `Ellipse` + `Title` `TextBlock` in `TaskListView.xaml`.

### F2 — Detail panel isn't gated on `SelectedTask != null`

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Adherence
- **Location**: src/BeaverWorks.Desktop/Views/TaskListView.xaml:28-64
- **Detail**: Phase 2's contract said the details block should be "shown only when `SelectedTask is not null`". The `StackPanel` in row 3 is always visible; with no selection it renders an empty title/description, an empty combo box, and "—" placeholders instead of being hidden. Not a functional bug (the "—" placeholders already added for cost/time/room prevent blank confusion), but it doesn't match the stated visibility contract.
- **Fix**: Bind the details `StackPanel`'s `Visibility` to `SelectedTask` via a null-to-visibility converter (same shape as the existing `NullOrEmptyStringToVisibilityConverter`, but checking object null instead of string empty).
- **Decision**: FIXED — added `NullToVisibilityConverter.cs` and bound the details `StackPanel`'s `Visibility` to `SelectedTask` in `TaskListView.xaml`.

### F3 — Delete-blocked check duplicated between `App.xaml.cs` and `ProjectWorkspaceViewModel.DeleteTask`

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Plan Adherence
- **Location**: src/BeaverWorks.Desktop/App.xaml.cs:122-131 (pre-check) and src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs:37, 97-101 (`DeleteBlocked` event + internal check)
- **Detail**: The plan's Phase 3 contract said `DeleteTask` should do the blocking check itself and `App.xaml.cs` should call `DeleteTask` then react to a reported block via a second message box. The actual implementation instead pre-checks `Project.GetDependents` in `App.xaml.cs` *before* asking for delete confirmation (so the confirm dialog never appears for a blocked task — this was an intentional fix during this session's manual-test triage, since the original literal-plan flow produced a confusing double dialog). This is a reasonable UX improvement, but it leaves `ProjectWorkspaceViewModel`'s own dependents-check and `DeleteBlocked` event effectively dead code — `DeleteTask` is now only ever called by `App.xaml.cs` after it has already confirmed there are no dependents, so `DeleteBlocked` can never fire in the current call graph. Confirmed via manual testing that only one dialog shows today, so there's no live user-facing bug — this is a maintainability/dead-code concern.
- **Fix A ⭐ Recommended**: Remove `DeleteBlocked` (event + subscription) and the internal dependents re-check from `ProjectWorkspaceViewModel.DeleteTask`, since `App.xaml.cs` is now the single source of truth for the blocking decision. Keep a defensive early-return in `DeleteTask` (e.g. `if (_project.GetDependents(taskId).Count > 0) return;`) with no event, purely as a safety net against future direct callers.
  - Strength: Removes dead code and the duplicated logic; matches how the UI actually behaves today (single check, single dialog) — no user-visible change.
  - Tradeoff: `ProjectWorkspaceViewModel` becomes slightly less self-contained (a future caller bypassing `App.xaml.cs`'s pre-check would silently no-op instead of getting an explicit signal).
  - Confidence: HIGH — matches the already-verified, already-shipped UI behavior; this is a cleanup, not a behavior change.
  - Blind spot: None significant — `DeleteRequested` has exactly one subscriber (`App.xaml.cs`) in the current codebase.
- **Fix B**: Keep `ProjectWorkspaceViewModel` as the single source of truth (remove `App.xaml.cs`'s pre-check), and restructure the confirm flow to ask "are you sure?" first, then only show the blocked-message via `DeleteBlocked` if the delete actually fails — reverting to the plan's originally literal design.
  - Strength: Matches the plan's Phase 3 contract exactly, keeps all delete-eligibility logic in one place (the view model).
  - Tradeoff: Reintroduces the UX regression the user explicitly flagged and asked to be fixed this session (an unnecessary confirm dialog before the user learns the delete is blocked).
  - Confidence: MEDIUM — functionally correct but goes against explicit user feedback already incorporated.
  - Blind spot: None significant.
- **Decision**: FIXED via Fix A — removed `DeleteBlocked` event, its `App.xaml.cs` subscription, and the message-naming logic from `DeleteTask`; kept a silent defensive early-return in `DeleteTask` if dependents exist.

### F4 — Project mutated in memory before `IProjectStore.Save`, no rollback on failure

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs:60-65 (`AddTask`), 81-86 (`UpdateTask`), 105-115 (`DeleteTask`)
- **Detail**: Each mutation method updates `_project.Tasks`/`_project.UpdatedAt` in memory, then calls `_projectStore.Save`, then refreshes `Canvas`/`TaskList` from the (already-mutated) in-memory `_project`. If `Save` throws (e.g. disk full, permissions, file locked), the in-memory project has already diverged from what's on disk, and the exception would propagate up through a WPF command/event handler with no user-facing error message. This ordering was inherited from the pre-existing `PlanCanvasViewModel.AddTask` pattern (this plan's own "Critical Implementation Details" section explicitly calls for mirroring that order), so it isn't a new problem introduced by this plan, but it's worth flagging as pre-existing technical debt this plan expanded to 3 more call sites (Update/Delete) instead of 1.
- **Fix**: Wrap each `_projectStore.Save` call in a try/catch; on failure, revert the in-memory mutation (or reload from the last-known-good state) and surface an error (e.g. a `MessageBox` in `App.xaml.cs` via a new `SaveFailed` event) instead of silently leaving canvas/list refreshed against unsaved state.
- **Decision**: FIXED — added a `TrySave` helper (catches `IOException`/`UnauthorizedAccessException`, raises `SaveFailed`) used by `AddTask`/`UpdateTask`/`DeleteTask`; each rolls back its in-memory mutation on failure. `App.xaml.cs` shows a "Save failed" `MessageBox` on `SaveFailed`.

### F5 — Numeric edit-dialog fields can silently keep stale values or throw on extreme input

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: src/BeaverWorks.Desktop/Views/EditTaskDialog.xaml:58,70-71 and src/BeaverWorks.Desktop/ViewModels/EditTaskViewModel.cs:103-149
- **Detail**: `Priority`/`EstimatedCost`/`EstimatedTimeHours` are plain `TextBox`es bound to numeric `[ObservableProperty]`s with no `ValidationRule`/converter. WPF's default behavior on a non-parseable string is to leave the underlying property unchanged without any visible error, so a user's clearly-invalid edit (e.g. typing "abc" over a priority) can silently no-op instead of surfacing `ErrorMessage`. Separately, an extreme `EstimatedTimeHours` value (e.g. very large) could throw inside `TimeSpan.FromHours` when `Save` runs, which isn't caught.
- **Fix**: Add basic bounded parsing/validation in `Save()` (or a WPF `ValidationRule`) that surfaces a clear `ErrorMessage` for non-numeric or out-of-range input instead of relying on silent binding failure or an uncaught exception.
- **Decision**: FIXED — added `ValidatesOnExceptions=True`/`NotifyOnValidationError=True` to the numeric bindings in `EditTaskDialog.xaml` (surfaces bad input via WPF's standard validation error border) and a `MaxEstimatedTimeHours` bound + try/catch around `TimeSpan.FromHours` in `EditTaskViewModel.Save()`.

### F6 — `TaskDependencyValidator.WouldCreateCycle` throws on duplicate task IDs

- **Severity**: OBSERVATION
- **Dimension**: Safety & Quality
- **Location**: src/BeaverWorks.Core/Services/TaskDependencyValidator.cs:24
- **Detail**: `project.Tasks.ToDictionary(t => t.Id)` throws `ArgumentException` if two tasks share an `Id` (e.g. from a hand-edited or corrupted project JSON file). Not reachable through normal UI flows (IDs are always `Guid.NewGuid()`), but there's no defensive handling if the project file is ever malformed.
- **Fix**: Build the lookup defensively (e.g. `GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.First())`) so a malformed file doesn't crash the validator.
- **Decision**: SKIPPED — not reachable through normal UI flows; accepted as low-priority risk for now.

## Additional notes (not findings)

- **Automated verification**: `dotnet build BeaverWorks.sln --no-restore` → 0 warnings, 0 errors. `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj --no-build` → 33/33 passed. Both re-confirmed independently during this review.
- **Manual verification**: All Progress-section manual checkboxes (Phase 2: 2.3-2.8, Phase 3: 3.3-3.8) are `[x]` with observable evidence — each was interactively confirmed by the human during implementation, including two rounds of bugs found and fixed live (marker-selection binding mode, canvas hit-testing, delete-confirmation double-dialog).
- **Scope Discipline**: No violations of the "What We're NOT Doing" list found — no marker drag-to-move, no position editing, no cascading delete, no undo/redo, no new automated Desktop/UI test project.
- **Selection-sync loop guard**: Verified correct — `TaskListViewModel.Select`/`Refresh` suppress `SelectionChanged` via `_suppressSelectionChanged`, and `PlanCanvasViewModel.SelectMarker` never raises `MarkerSelected` (only `NotifyMarkerClicked` does), so canvas↔list sync cannot loop.
- **Status-dropdown double-invoke risk**: Verified safe — `TaskListView.xaml.cs`'s `SelectionChanged` handler re-fires when `SelectedTask` changes programmatically, but `TaskListViewModel.ChangeStatus`'s no-op guard (`SelectedTask.Status == newStatus`) prevents any duplicate persistence/UI work.
