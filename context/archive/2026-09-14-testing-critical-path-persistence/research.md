---
date: 2026-09-14T09:55:00+02:00
researcher: Copilot CLI
git_commit: c1c6fc8ad45747e7a10265d075670ef601810725
branch: main
repository: WSendys/beaver-works
topic: "Critical-path persistence coverage — Risk #1 (create/open project -> pin task -> save -> reopen -> task + marker position preserved end-to-end)"
tags: [research, codebase, persistence, project-workspace-viewmodel, plan-coordinate-mapper, project-store]
status: complete
last_updated: 2026-09-14
last_updated_by: Copilot CLI
---

# Research: Critical-path persistence coverage (Test Plan Phase 1, Risk #1)

**Date**: 2026-09-14T09:55:00+02:00
**Researcher**: Copilot CLI
**Git Commit**: c1c6fc8ad45747e7a10265d075670ef601810725
**Branch**: main
**Repository**: WSendys/beaver-works

## Research Question

Ground the Phase 1 rollout of `context/foundation/test-plan.md` ("Critical-path persistence coverage", Risk #1): after create/open project → click plan → enter task title → save → reload with a **new** store/view-model instance (not a re-read variable), does the reloaded project expose the task with an identical title and identical marker coordinates within the established rounding margin? Specifically, challenge the assumption that the existing unit tests for `PlanCoordinateMapper`, the project model, and the JSON store already prove this end-to-end, and identify:
- the entry point for "click plan → create task" (which command/handler),
- the exact project save/load round trip (which store, which serialization type),
- what "reload" means safely in a test (new instance vs. cached),
- the established coordinate rounding margin.

## Summary

The pieces are real but **nothing today exercises the full chain in one test**. The existing tests each cover one link:
- `PlanCoordinateMapperTests` proves click→plan-coordinate math (with `precision: 10`, i.e. essentially exact).
- `ProjectStoreTests.SaveAndLoad_RoundTripsProjectWithTasks_PositionPreservedExactly` proves JSON save→load round-trips a task's title/position **exactly**, using the same `ProjectStore` instance in the same test method.
- `RenovationTaskTests` proves the model itself holds a `PlanPoint` without mutation.

None of them exercises `ProjectWorkspaceViewModel.AddTask` (the actual save trigger used by the real UI flow) or constructs a **second, independent** `ProjectWorkspaceViewModel`/`ProjectStore` instance against the same file path to prove a genuine reload (as opposed to just re-deserializing in the same test method, which the existing `ProjectStoreTests` already does one level below the ViewModel). So the assumption that "the pieces already prove this end-to-end" is **false at the ViewModel/save-trigger layer**: nothing today proves that clicking-to-create and `AddTask`'s auto-save path actually round-trips through a fresh `ProjectWorkspaceViewModel`.

There is **no explicit "rounding margin" constant** in production code — `PlanPoint` is documented as full double precision with "no rounding." The closest thing to an established margin is the `precision: 10` used in `PlanCoordinateMapperTests` (i.e., xUnit's `Assert.Equal(double, double, precision: 10)`, effectively exact-match tolerance for floating point). The integration test should adopt the same `precision: 10` (or exact equality, consistent with `ProjectStoreTests`) rather than inventing a new tolerance.

## Detailed Findings

### 1. Entry point: "click plan → pin task"

- UI click is captured in code-behind and forwarded to the ViewModel as a raw position: `PlanCanvasView.xaml.cs:28-33`, wired from `PlanImage_MouseLeftButtonDown` in `PlanCanvasView.xaml:13-18,24-27,36-37`.
- `PlanCanvasViewModel.HandleClick(...)` (`src/BeaverWorks.Desktop/ViewModels/PlanCanvasViewModel.cs:88-102`) converts the raw click into a normalized plan point via `PlanCoordinateMapper.TryMapClickToPlan(...)`; a non-null result raises `NewTaskRequested(position)`.
- `ProjectWorkspaceViewModel` relays `Canvas.NewTaskRequested -> NewTaskRequested` (`ProjectWorkspaceViewModel.cs:63-64`); `App.xaml.cs:90-110` subscribes to that event and opens `NewTaskDialog`.
- `NewTaskViewModel.Create()` (`src/BeaverWorks.Desktop/ViewModels/NewTaskViewModel.cs:63-108`) builds the task via `RenovationTask.Create(title, position, ...)`, resolves dependency status, and raises `TaskCreated`.
- `App.xaml.cs:107-110` wires `newTaskViewModel.TaskCreated += (_, task) => workspaceViewModel.AddTask(task);` — this is the actual save trigger for a newly pinned task.

**Conclusion**: the integration test's "click plan → enter task title" step should be simulated by directly calling `ProjectWorkspaceViewModel.AddTask(RenovationTask.Create(title, position))` (bypassing the WPF click/dialog machinery, which is UI-automation territory out of scope per the test-plan's Stack section) — this is the narrowest point that still exercises the real save-trigger code path used by production.

### 2. Save trigger and mutation flow

- **No explicit Save command exists.** Save is automatic on every mutation:
  - `AddTask(task)` mutates `_project.Tasks`, sets `_project.UpdatedAt`, calls `TrySave()`; on failure it rolls back the in-memory add (`ProjectWorkspaceViewModel.cs:75-88`).
  - `UpdateTask(...)` and `DeleteTask(...)` follow the same mutate → `TrySave()` → refresh-panels pattern (`ProjectWorkspaceViewModel.cs:103-176`, `198-213`).
  - `TrySave()` calls `_projectStore.Save(_project, _projectFilePath)`, converting `IOException`/`UnauthorizedAccessException` into a `SaveFailed` event rather than propagating (`ProjectWorkspaceViewModel.cs:221-231`).
- After a successful save, `Canvas.AddMarker(...)`/`UpdateMarker(...)`/`RemoveMarker(...)` and `TaskList.Refresh(_project.Tasks)` keep both panels in sync (`ProjectWorkspaceViewModel.cs:86-88`, `163-176`, `211-213`).

### 3. "Reopen" / genuine reload semantics

- `ProjectWorkspaceViewModel` is constructed with `(Project project, string projectFilePath, IProjectStore projectStore, string username, IUserBudgetProfileStore budgetProfileStore)` (`ProjectWorkspaceViewModel.cs:52`) — it takes an **already-loaded** `Project` object, not a file path to load itself.
- The actual "open project" / load step lives one level up, in `RecentProjectsViewModel.TryOpen(filePath)`, which calls `_projectStore.Load(filePath)` (`RecentProjectsViewModel.cs:63-97`); `App.xaml.cs` then constructs a new `ProjectWorkspaceViewModel` from that loaded `Project` (`App.xaml.cs:90-97`).
- `ProjectStore.Load` (`src/BeaverWorks.Core/Persistence/ProjectStore.cs:14-41`) reads the file fresh with `File.ReadAllText` + `JsonSerializer.Deserialize<Project>(json, SerializerOptions)` — **no caching, no singleton, no static state**. Each call returns a brand-new object graph.
- **Implication for the test**: "reload with a new instance" is satisfied by constructing a **second** `ProjectStore` (or reusing the class but calling `.Load` again with a fresh path/instance — either is safe since there's no cache) and a **second** `ProjectWorkspaceViewModel` from the freshly-loaded `Project`, rather than reusing the original `_project` reference held by the first ViewModel. The existing `ProjectStoreTests` round-trip test already reloads via the same store instance in the same test method — the store itself has no state to reset, so instance reuse there is not a validity concern, but the integration test should still construct a **new `ProjectWorkspaceViewModel`** wrapping the reloaded `Project` to prove the full ViewModel-level flow, since that's the thing not yet tested.

### 4. Project model / marker coordinate types

- `Project` (`src/BeaverWorks.Core/Models/Project.cs:7-19`): `Name`, `PlanImagePath`, `Tasks` (`List<RenovationTask>`), `CreatedAt`/`UpdatedAt`.
- `RenovationTask` (`src/BeaverWorks.Core/Models/RenovationTask.cs:9-39,52-56`): `Title` (required), `Status` (default `Planned`), `Position` (required `PlanPoint`), plus `Id`, `Description`, `Priority`, `EstimatedCost`, `EstimatedTime`, `RoomId`, `DependsOnTaskIds`, timestamps. `Create(title, position, ...)` is the factory used by both `NewTaskViewModel` and tests.
- `PlanPoint` (`src/BeaverWorks.Core/Models/PlanPoint.cs:3-12`): normalized `(0-1)` `X`/`Y` as `double`; doc comment: "Stored at full double precision — no rounding — so a save/load round-trip never drifts."

### 5. JSON store / serialization

- `ProjectStore` (`src/BeaverWorks.Core/Persistence/ProjectStore.cs:14-41`):
  - `SerializerOptions = new() { WriteIndented = true }` — `System.Text.Json`, default converters, no custom coordinate converter.
  - `Save(Project, string projectFilePath)`: creates parent directory if missing, serializes, `File.WriteAllText`.
  - `Load(string projectFilePath)`: `File.ReadAllText` → `JsonSerializer.Deserialize<Project>` → throws `InvalidDataException` if null; validates `PlanImagePath` exists on disk, else throws `PlanImageMissingException`.
  - `.bwproj` is the stated file-extension convention (`ProjectStore.cs:9`) but not enforced — any path works, including a temp path.
- `IProjectStore` (`src/BeaverWorks.Core/Persistence/IProjectStore.cs:8-19`) exposes exactly `Load`/`Save`, both usable directly for an integration test's own store instance(s).

### 6. Existing tests and their actual coverage boundary

- `PlanCoordinateMapperTests.cs:17-96` — covers click→plan math only: inside-image mapping, letterbox-margin rejection (`null`), exact-boundary inclusion, reverse round-trip via `MapPlanToControl`, and zero/negative dimension guards. Coordinate assertions use `Assert.Equal(expected, actual, precision: 10)` (e.g. `PlanCoordinateMapperTests.cs:26-27,64-69,84-85`). **Does not touch `Project`, `ProjectStore`, or any ViewModel.**
- `ProjectStoreTests.cs:12-18,29-50` — `SaveAndLoad_RoundTripsProjectWithTasks_PositionPreservedExactly` builds a `Project` with one `RenovationTask` (`PlanPoint { X = 0.123456789, Y = 0.987654321 }`), saves and loads via **direct `ProjectStore` calls** (bypassing any ViewModel), and asserts exact `Title`/`Position.X`/`Position.Y`/`Status` equality. Also covers `Load_MissingPlanImage_ThrowsPlanImageMissingExceptionAndLeavesProjectFileUntouched` (`ProjectStoreTests.cs:53-71`). **Does not touch `ProjectWorkspaceViewModel.AddTask`/`TrySave`, i.e. never exercises the actual production save trigger or the auto-refresh-after-save behavior.**
- `RenovationTaskTests.cs:10,17-18` — asserts `PlanPoint { X = 0.5, Y = 0.5 }` stored on a task via `Create(...)` is held unchanged; exact equality, no persistence involved.

**This confirms the plan's "must challenge" framing**: the unit tests prove the pieces (coordinate math, JSON round-trip, model immutability of position) but never wire them through `ProjectWorkspaceViewModel.AddTask` → `TrySave()` → a **second, independently constructed** `ProjectWorkspaceViewModel`/`ProjectStore` pair reading the same file back. That gap is exactly Risk #1.

### 7. Rounding margin

- No named constant exists in `src/BeaverWorks.Core` for a coordinate tolerance (`Math.Round`, epsilon, magic number) — searched project-wide, only hits were the letterbox-margin boundary logic in `PlanCoordinateMapper` (an unrelated concept — margin around the *rendered image*, not a coordinate-comparison tolerance) and `PlanPoint`'s "no rounding" doc comment.
- The only explicit numeric precision tied to plan coordinates in the codebase is `precision: 10` in `PlanCoordinateMapperTests` (`PlanCoordinateMapperTests.cs:26-27,64-69,84-85`), which for xUnit's `Assert.Equal(double, double, int precision)` means agreement to 10 decimal digits — effectively exact for any double a UI click could produce.
- PRD guardrail (`context/foundation/prd.md:65-68`, Polish): "Zapis/odczyt projektu nigdy nie traci ani nie przesuwa danych zadań (pozycja markera na planie musi pozostać dokładnie taka sama po zapisie i ponownym otwarciu)" — i.e. save/read must never lose or shift task data; marker position must remain **exactly** the same after save and reopen.
- PRD acceptance criterion (`context/foundation/prd.md:91-93`, Polish): "Zapisany i ponownie otwarty projekt odtwarza zadanie z identyczną pozycją geometrii (współrzędne 0–1 bez utraty precyzji poza ustalonym marginesem zaokrąglenia)" — reloaded project reproduces the task with identical geometry, coordinates 0–1 without precision loss beyond an "established rounding margin."

**Conclusion**: despite the PRD wording implying a margin, no such margin is implemented or documented as a number anywhere except the `precision: 10` test convention. The integration test should assert exact equality (or `precision: 10` for consistency with `PlanCoordinateMapperTests`) rather than invent a new tolerance value — since `System.Text.Json` round-trips `double` losslessly and `PlanPoint` performs no rounding, exact equality is the correct and currently-true behavior to assert.

## Code References

- `src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs:22,52,61-68,75-88,103-176,198-213,221-231` — composition root; `AddTask`/`UpdateTask`/`DeleteTask`/`TrySave`; the actual save trigger.
- `src/BeaverWorks.Desktop/ViewModels/PlanCanvasViewModel.cs:88-102` — click → `PlanCoordinateMapper` → `NewTaskRequested`.
- `src/BeaverWorks.Desktop/ViewModels/NewTaskViewModel.cs:63-108` — `RenovationTask.Create(title, position, ...)`, raises `TaskCreated`.
- `src/BeaverWorks.Desktop/App.xaml.cs:90-110` — wires `NewTaskDialog.TaskCreated -> workspaceViewModel.AddTask`.
- `src/BeaverWorks.Desktop/ViewModels/RecentProjectsViewModel.cs:63-97` — `TryOpen(filePath)` → `_projectStore.Load(filePath)`; the real "open/reopen" entry point.
- `src/BeaverWorks.Core/Models/Project.cs:7-19` — `Project` model.
- `src/BeaverWorks.Core/Models/RenovationTask.cs:9-39,52-56` — `RenovationTask` model + `Create` factory.
- `src/BeaverWorks.Core/Models/PlanPoint.cs:3-12` — marker coordinate type; "no rounding" doc comment.
- `src/BeaverWorks.Core/Persistence/ProjectStore.cs:9,14-41` — `Save`/`Load`, `System.Text.Json`, no caching.
- `src/BeaverWorks.Core/Persistence/IProjectStore.cs:8-19` — store interface.
- `tests/BeaverWorks.Core.Tests/Persistence/ProjectStoreTests.cs:12-18,29-50,53-71` — existing save/load round-trip test (bypasses ViewModel).
- `tests/BeaverWorks.Core.Tests/Services/PlanCoordinateMapperTests.cs:17-96` — click→coordinate math tests; `precision: 10` convention.
- `tests/BeaverWorks.Core.Tests/Models/RenovationTaskTests.cs:10,17-18` — model-level position assertion.
- `context/foundation/prd.md:65-68,91-93` — save/read guardrail and acceptance criterion (Polish; quoted above).

## Architecture Insights

- **Single-save-point convention**: `ProjectWorkspaceViewModel` is documented (`ProjectWorkspaceViewModel.cs:9-18`) as the sole place that talks to `IProjectStore` for an open project — "Neither child view model talks to `IProjectStore` directly." An integration test that wants to prove the real flow must go through this class's public mutation methods (`AddTask`), not call `ProjectStore.Save` directly (that's what the existing, insufficient `ProjectStoreTests` already does).
- **Auto-save, no explicit Save command**: every mutation is immediately persisted with rollback-on-failure; there is no "dirty state" to flush before "closing" a project. This simplifies the test: "save" already happened by the time `AddTask` returns.
- **No caching in `ProjectStore`**: safe to construct a brand-new `ProjectStore` (or reuse the class, since it holds no mutable state) for the "reload" side of the test — the critical thing being tested is a **new `Project` object graph** and (per the change's stated intent) a **new `ProjectWorkspaceViewModel` instance**, not a new `ProjectStore` class necessarily.
- **No rounding implemented anywhere** — `PlanPoint` and `System.Text.Json` double serialization are exact; the PRD's "established rounding margin" phrase has no corresponding code constant. Test should assert exact equality (mirroring `ProjectStoreTests`) or adopt `precision: 10` (mirroring `PlanCoordinateMapperTests`) for stylistic consistency — either is currently true.

## Historical Context (from prior changes)

- `context/archive/2026-09-13-pin-and-persist-task/plan.md:40-43,50-52,378-401,496-502` — original feature plan: click pins a task with a "Planned"-colored marker; states the position must "survive save, close, and reopen"; describes the same click → dialog → persist chain confirmed above; **explicitly scoped automated tests to Core only, leaving the Desktop/ViewModel layer covered only by manual verification steps** — this is the direct historical source of today's gap (only pieces are unit-tested, nothing end-to-end).
- `context/archive/2026-09-14-polish-core-workflows/plan.md:20,50,188` — confirms `UpdateTask` save/refresh ordering (budget consumption before recompute) that the integration test must not disturb if it also exercises status transitions (not required for Risk #1, but relevant if the test reuses `AddTask`/`UpdateTask` helpers).
- `context/changes/testing-critical-path-persistence/change.md:15` — this change's own stated intent, consistent with the above.

## Related Research

- None found under `context/changes/**/research.md` or `context/archive/**/research.md` — no prior research artifact targeted this specific end-to-end persistence gap; the archived `pin-and-persist-task/plan.md` is a planning document, not a research document, but is the closest prior art.

## Open Questions

- Should the integration test simulate "click plan" via `PlanCanvasViewModel.HandleClick` (exercising `PlanCoordinateMapper` too) or start directly from a known `PlanPoint` and call `ProjectWorkspaceViewModel.AddTask` (narrower, avoids re-testing coordinate math already covered by `PlanCoordinateMapperTests`)? Test-plan's Risk Response Guidance suggests the ViewModel/command-wiring gap is the priority, so starting from `AddTask` is likely sufficient and cheaper — recommend confirming this scope decision in `/10x-plan`.
- Should the test also cover `RecentProjectsViewModel.TryOpen` as the "reopen" entry point, or is constructing a second `ProjectWorkspaceViewModel` directly from a second `ProjectStore.Load(...)` call sufficient to prove the risk without pulling in `App.xaml.cs`/dialog wiring (out of integration-test scope, UI-automation territory)? Recommend the latter for cost × signal, per `/10x-plan` to confirm.
- `IUserBudgetProfileStore` and `username` are required constructor params for `ProjectWorkspaceViewModel` — the test will need a real or minimal budget-profile store fixture; confirm during planning whether an existing test double/fixture already exists in `tests/BeaverWorks.Core.Tests` for this dependency (not directly investigated here since it's outside Risk #1's scope, but needed to compile the test).
