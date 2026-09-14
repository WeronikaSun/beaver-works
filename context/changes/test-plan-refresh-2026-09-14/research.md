---
date: 2026-09-14T11:35:00+02:00
researcher: Copilot CLI
git_commit: 95143e524d97f11f7ce23dae66d4937d48e5b4ee
branch: main
repository: WSendys/beaver-works
topic: "Refresh test-plan.md — add Risk #2 (ProjectWorkspaceViewModel save/refresh regression) as rollout Phase 2"
tags: [research, codebase, project-workspace-viewmodel, task-list-viewmodel, plan-canvas-viewmodel, test-plan]
status: complete
last_updated: 2026-09-14
last_updated_by: Copilot CLI
---

# Research: Grounding Risk #2 (ProjectWorkspaceViewModel save/refresh regression) for test-plan.md Phase 2

**Date**: 2026-09-14T11:35:00+02:00
**Researcher**: Copilot CLI
**Git Commit**: 95143e524d97f11f7ce23dae66d4937d48e5b4ee
**Branch**: main
**Repository**: WSendys/beaver-works

## Research Question

The author-stated single-test scope override in `context/foundation/test-plan.md` §1 no longer applies. We need to add Risk #2 (`ProjectWorkspaceViewModel` mutation/save/refresh regression) as a new rollout Phase 2 (minimal scope, one additional integration test, no E2E/UI automation). Before writing the plan, ground:

1. Which command drives task edit/remove.
2. How `TaskListViewModel`/`PlanCanvasViewModel` observe `Project.Tasks`.
3. The save-then-refresh ordering.
4. The cheapest test layer and how to avoid the "asserts only the underlying collection changed" anti-pattern.

## Summary

- **Edit** is triggered by `TaskListViewModel.Edit()` (a `[RelayCommand]`) raising `EditRequested`, wired in `App.xaml.cs` to `ShowEditTaskDialog(...)`, which ultimately calls `ProjectWorkspaceViewModel.UpdateTask(RenovationTask)`. **Delete** is triggered by `TaskListViewModel.Delete()` raising `DeleteRequested`, wired to `ConfirmAndDeleteTask(...)` → `ProjectWorkspaceViewModel.DeleteTask(Guid)`. Both real command bodies (mutate → save → refresh panels) live on `ProjectWorkspaceViewModel`, not on `TaskListViewModel` itself.
- **Neither child panel observes `Project.Tasks` live.** There is no `INotifyCollectionChanged` subscription anywhere in this chain. `TaskListViewModel` owns its own `ObservableCollection<RenovationTask> Tasks` populated by an explicit `Refresh(IEnumerable<RenovationTask>)` call from the parent. `PlanCanvasViewModel` owns its own `ObservableCollection<TaskMarkerViewModel> Markers`, updated only via explicit `AddMarker`/`UpdateMarker`/`RemoveMarker` calls from the parent. **This is exactly the assumption the risk statement told us to challenge — confirmed true: in-memory mutation of `Project.Tasks` does NOT imply the panels refresh; the parent must explicitly push the update to each panel.**
- **Save happens before the panel refresh, and only on success.** In both `UpdateTask` and `DeleteTask`, the order is: mutate `_project.Tasks` → set `_project.UpdatedAt` → call `TrySave()` (synchronous, wraps `IProjectStore.Save`) → **only if `TrySave()` returns true**: push updates to `Canvas` (`UpdateMarker`/`RemoveMarker`) and call `TaskList.Refresh(_project.Tasks)`. On save failure, the mutation is rolled back and panels are never touched. There is no async gap and no separate event-ordering mechanism — it's a single synchronous call sequence in one method body.
- **Cheapest sufficient layer is integration**, same style as the existing Phase 1 test: construct `ProjectWorkspaceViewModel` directly (no UI automation), call `UpdateTask(...)` (or `DeleteTask(...)`), then assert three things — not just one — to avoid the "mirror implementation" / "asserts only the collection changed" anti-pattern:
  1. the change is present in `_project.Tasks` (via `workspace.Project.Tasks`),
  2. it is present in `workspace.TaskList.Tasks` (the panel's own observable collection, not the same reference as `Project.Tasks`),
  3. it is present/updated in `workspace.Canvas.Markers` (the panel's own observable collection),
  4. **and** it survives a reload — reload via a fresh `ProjectStore.Load(...)` + a fresh second `ProjectWorkspaceViewModel` instance — same convention as Phase 1.
- The existing Phase 1 test (`ProjectWorkspacePersistenceTests.cs`) already establishes the exact fixture/constructor/reload conventions a Phase 2 test must reuse: real-PNG fixture copy, direct VM construction, `ProjectStore()` reload into a second VM instance, `precision: 10` for coordinate comparisons.

## Detailed Findings

### Edit/Delete command wiring

- `TaskListViewModel.Edit()` / `TaskListViewModel.Delete()` are `[RelayCommand]`s that only raise events — they do not mutate or persist anything themselves (`src/BeaverWorks.Desktop/ViewModels/TaskListViewModel.cs:196-211`).
- `ProjectWorkspaceViewModel`'s constructor wires the panels together and forwards `TaskList.StatusChangeRequested` to `UpdateTaskStatus` (`src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs:52-68`).
- `App.xaml.cs` wires `TaskList.EditRequested` → `ShowEditTaskDialog(workspaceViewModel, taskId)` and `TaskList.DeleteRequested` → `ConfirmAndDeleteTask(workspaceViewModel, taskId)` (`src/BeaverWorks.Desktop/App.xaml.cs:92-110, 116-126`). These dialog handlers are the production callers of `UpdateTask`/`DeleteTask`.
- The actual, testable mutation+persistence+refresh logic is `ProjectWorkspaceViewModel.UpdateTask(RenovationTask)` (`ProjectWorkspaceViewModel.cs:101-177`) and `ProjectWorkspaceViewModel.DeleteTask(Guid)` (`ProjectWorkspaceViewModel.cs:187-214`). A Phase 2 test should call these methods directly (mirroring how the Phase 1 test calls `AddTask(...)` directly rather than driving the UI), consistent with the "integration, no UI automation" cheapest-layer choice.

### `Project.Tasks` is the source of truth; panels do not observe it

- `ProjectWorkspaceViewModel` exposes `Project` directly and does not wrap `Project.Tasks` in its own collection (`ProjectWorkspaceViewModel.cs:33-37, 52-61`).
- `TaskListViewModel` holds its own `ObservableCollection<RenovationTask> Tasks` (plus `Rows`/`DoneRows`) populated only via `Refresh(IEnumerable<RenovationTask>)`, called explicitly by the parent after a successful save (`TaskListViewModel.cs:19-30, 81-104`).
- `PlanCanvasViewModel` holds its own `ObservableCollection<TaskMarkerViewModel> Markers`, updated only via `AddMarker`/`UpdateMarker`/`RemoveMarker`, called explicitly by the parent (`PlanCanvasViewModel.cs:33-41, 135-159`).
- No `CollectionChanged` subscription exists anywhere in this chain — confirmed by full read of all three ViewModels. This directly confirms the risk statement's must-challenge assumption: **an in-memory mutation of the model does not, by itself, propagate to either panel.** A test that only checks `workspace.Project.Tasks` after calling `UpdateTask`/`DeleteTask` would pass even if the `Canvas.UpdateMarker(...)`/`TaskList.Refresh(...)` calls were accidentally deleted from the method body — this is the exact "asserts only the underlying collection changed" anti-pattern the Notes call out.

### Save happens before panel refresh, synchronously, only on success

- `UpdateTask`: mutate `_project.Tasks[index]` → set `_project.UpdatedAt` → `TrySave()` → **on success only**: `Canvas.UpdateMarker(task)`, `TaskList.Refresh(_project.Tasks)`, budget adjustments, `RecomputeRecommendations()`. On failure, the mutation (and any cascaded dependency-status changes) is rolled back and panels are untouched (`ProjectWorkspaceViewModel.cs:101-177`).
- `DeleteTask`: remove from `_project.Tasks` → set `_project.UpdatedAt` → `TrySave()` → on success: `Canvas.RemoveMarker(taskId)`, `TaskList.Refresh(_project.Tasks)`, `RecomputeRecommendations()`; on failure, re-insert the removed task and skip panel updates (`ProjectWorkspaceViewModel.cs:187-214`).
- `TrySave()` is a synchronous wrapper around `IProjectStore.Save(_project, _projectFilePath)`, catching `IOException`/`UnauthorizedAccessException` and raising `SaveFailed` on failure (`ProjectWorkspaceViewModel.cs:221-233`). There is no async gap between save and refresh — a Phase 2 integration test does not need to await/poll; the call sequence is deterministic and in-process.
- `AddTask(...)` (used by the existing Phase 1 test) follows the identical add→save→refresh pattern (`ProjectWorkspaceViewModel.cs:75-89`), confirming this ordering is a consistent convention across all three mutation entry points, not specific to add.

### Test-double/DI surface a Phase 2 test needs

- `ProjectWorkspaceViewModel(Project project, string projectFilePath, IProjectStore projectStore, string username, IUserBudgetProfileStore budgetProfileStore)` — same signature the Phase 1 test already uses (`ProjectWorkspaceViewModel.cs:52-61`; used at `ProjectWorkspacePersistenceTests.cs:52-55`).
- `TaskListViewModel(IEnumerable<RenovationTask> initialTasks)` and `PlanCanvasViewModel(Project project)` are constructed internally by `ProjectWorkspaceViewModel`, not directly by tests — a Phase 2 test reads them off `workspace.TaskList` / `workspace.Canvas`, it does not construct them separately.
- `IProjectStore` is implemented by the real `ProjectStore` (JSON file-backed, `System.Text.Json`, no caching) — the Phase 1 test uses the real implementation rather than a mock/stub, and Phase 2 should follow the same convention (integration layer, real persistence, no mocking — matches `test-plan.md` §4 "mocking/fixtures: none").
- `PlanCanvasViewModel`'s constructor eagerly decodes `Project.PlanImagePath` via `BitmapImage.BeginInit()/EndInit()` — any test constructing `ProjectWorkspaceViewModel` needs a real, decodable PNG at that path (already documented in `test-plan.md` §6.2 and already solved by the Phase 1 test fixture).

### Existing Phase 1 test — exact conventions a Phase 2 test must reuse

`tests/BeaverWorks.Desktop.Tests/ProjectWorkspacePersistenceTests.cs`:
- Constructor copies a real PNG fixture (`AppContext.BaseDirectory\Assets\sample-floor-plan.png`) into a per-test temp directory (`ProjectWorkspacePersistenceTests.cs:25-33`).
- Builds a `Project` with `Name`, `PlanImagePath`, `CreatedAt` (`:46-52`).
- Constructs `ProjectWorkspaceViewModel` with `new ProjectStore()`, username `"test-user"`, `new UserBudgetProfileStore(_tempDirectory)` (`:52-55`).
- Creates a task via `RenovationTask.Create(title, position)` (`:57-59`).
- Calls the real production mutation method directly — `workspace.AddTask(task)` (`:71`) — no UI automation.
- Reloads via a **fresh** `new ProjectStore().Load(_projectFilePath)` and a **second** `ProjectWorkspaceViewModel` instance (`:73-75`) — this is the "genuinely new instance, not just re-reading a variable" convention.
- Asserts: exactly one task (`Assert.Single`), title equality, and X/Y equality with `precision: 10` (`:79-82`).
- Test project: `tests/BeaverWorks.Desktop.Tests/BeaverWorks.Desktop.Tests.csproj` — `net10.0-windows`, `UseWPF=true`, `IsPackable=false`; packages `xunit 2.9.3`, `xunit.runner.visualstudio 3.1.4`, `Microsoft.NET.Test.Sdk 17.14.1`, `coverlet.collector 6.0.4`; `ProjectReference` to `BeaverWorks.Desktop.csproj`; `sample-floor-plan.png` copied as content with `CopyToOutputDirectory=PreserveNewest`.
- Confirmed currently passing: `dotnet test tests/BeaverWorks.Desktop.Tests/BeaverWorks.Desktop.Tests.csproj --no-restore` → `Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1`.

### Persistence mechanism (`ProjectStore`)

- `src/BeaverWorks.Core/Persistence/ProjectStore.cs:12-41` — JSON-file-backed, `System.Text.Json`, `WriteIndented=true`, no caching. `Load` reads the file, deserializes, and throws `PlanImageMissingException` if `PlanImagePath` doesn't exist on disk. `Save` creates the parent directory if needed and writes indented JSON. Each `Load` call returns a fresh object graph — this is what makes "construct a second `ProjectWorkspaceViewModel` from a fresh `Load`" a valid, non-mirrored reload check.

## Code References

- `src/BeaverWorks.Desktop/ViewModels/TaskListViewModel.cs:196-211` — `Edit`/`Delete`/`ChangeStatus` `[RelayCommand]`s, event-raising only.
- `src/BeaverWorks.Desktop/ViewModels/TaskListViewModel.cs:19-30, 81-104` — `Tasks`/`Rows`/`DoneRows` collections and `Refresh(...)`.
- `src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs:33-37, 52-68` — `Project`/`Canvas`/`TaskList` exposure and ctor wiring.
- `src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs:75-89` — `AddTask` (existing Phase 1 save trigger).
- `src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs:101-177` — `UpdateTask` (edit mutate→save→refresh, with rollback and cascaded dependency-status handling).
- `src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs:187-214` — `DeleteTask` (delete mutate→save→refresh, with rollback).
- `src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs:221-233` — `TrySave()` synchronous wrapper around `IProjectStore.Save`.
- `src/BeaverWorks.Desktop/ViewModels/PlanCanvasViewModel.cs:33-41, 135-159` — `Markers` collection and `AddMarker`/`UpdateMarker`/`RemoveMarker`.
- `src/BeaverWorks.Desktop/App.xaml.cs:92-110, 116-126` — production wiring from `TaskList.EditRequested`/`DeleteRequested` to `UpdateTask`/`DeleteTask` via dialog handlers.
- `src/BeaverWorks.Core/Persistence/ProjectStore.cs:12-41` — JSON save/load implementation, no caching.
- `tests/BeaverWorks.Desktop.Tests/ProjectWorkspacePersistenceTests.cs:25-82` — Phase 1 reference test (fixture, construction, reload, assertions).
- `tests/BeaverWorks.Desktop.Tests/BeaverWorks.Desktop.Tests.csproj:1-26` — test project shape (TFM, packages, project reference, fixture content).
- `context/foundation/test-plan.md:1-140` — current strategy, risk map, Phase 1 rollout row, cookbook §6.2.

## Architecture Insights

- **Explicit-push, not observable-binding, is the propagation convention in this codebase.** Every panel ViewModel (`TaskListViewModel`, `PlanCanvasViewModel`) is deliberately decoupled from `Project.Tasks` via its own `ObservableCollection`, refreshed only by explicit parent calls after a successful save. This is a repo-wide pattern, not an oversight — it is consistent across `AddTask`, `UpdateTask`, and `DeleteTask`.
- **Persist-before-propagate with rollback-on-failure is the mutation convention.** All three mutation entry points on `ProjectWorkspaceViewModel` follow the same shape: mutate in-memory → save → only propagate to panels and recompute derived state (budget, recommendations) on save success; roll back the in-memory mutation on save failure. A Phase 2 test's assertions should reflect this: after a successful save, all three views (model, task list, canvas markers) must agree, and after reload, the same must hold from a fresh instance.
- **Real persistence + real fixtures over mocks is the existing integration-test convention** (`tests/BeaverWorks.Desktop.Tests`), matching `test-plan.md` §4's explicit "mocking/fixtures: none" stack row. A Phase 2 test should not introduce a stub `IProjectStore` — use the real `ProjectStore` against a temp path, exactly like Phase 1.

## Historical Context (from prior changes)

- `context/archive/2026-09-14-testing-critical-path-persistence/plan.md` — Phase 1 plan; already documents that `ProjectWorkspaceViewModel` can be constructed directly in tests (no UI wiring needed), that `AddTask(...)` is the real save trigger, that `PlanCanvasViewModel` eagerly decodes the plan image (real PNG fixture required), and the exact new-test-project contract (`net10.0-windows`, `UseWPF=true`, `ProjectReference` to Desktop) that a Phase 2 test should reuse rather than re-derive.
- `context/archive/2026-09-14-testing-critical-path-persistence/research.md` — originally identified `ProjectWorkspaceViewModel` as "the single point where every task mutation is saved and both child panels are refreshed" — this is the same file test-plan.md §2 Risk #2 cites as evidence, and this research confirms that description is accurate at the code level for `UpdateTask`/`DeleteTask` as well as `AddTask`.
- `context/foundation/test-plan.md` §2 Risk #2 row and Hot-spot churn note (`ProjectWorkspaceViewModel.cs` — 7 commits/30d) — already-recorded risk being promoted from deferred to an active Phase 2 by this change; no new risk discovery was needed, only grounding of the mechanism.

## Related Research

- `context/archive/2026-09-14-testing-critical-path-persistence/research.md` — Phase 1 research (predecessor; same file/pattern family).
- `context/archive/2026-09-14-testing-critical-path-persistence/plan.md` — Phase 1 plan (test project scaffold, fixture conventions to reuse).

## Open Questions

- None blocking. The plan can proceed directly from this research: use `UpdateTask` (edit) and/or `DeleteTask` (remove) as the mutation entry point, assert `Project.Tasks`, `TaskList.Tasks`, and `Canvas.Markers` all reflect the change, then reload via a fresh `ProjectStore.Load` + second `ProjectWorkspaceViewModel` and repeat the three-way assertion. Whether the Phase 2 test should cover edit, delete, or both in one test (vs. two smaller tests) is a plan-level scoping decision, not a research gap — `/10x-plan` should decide based on "minimal scope, one additional integration test."
