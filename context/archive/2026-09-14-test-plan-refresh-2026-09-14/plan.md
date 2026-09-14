# Test-Plan Refresh + Risk #2 Rollout Phase Implementation Plan

## Overview

Retire the author-stated single-test scope override in `context/foundation/test-plan.md` §1, promote Risk #2 (`ProjectWorkspaceViewModel` mutation/save/refresh regression) from deferred to an active rollout phase (Phase 2), and implement the one new integration test that proves it: a task mutation via `ProjectWorkspaceViewModel.UpdateTask`/`DeleteTask` is persisted via save AND reflected in both dependent panel ViewModels (`TaskList`, `Canvas`) — not just in the underlying `Project.Tasks` model.

## Current State Analysis

- `context/foundation/test-plan.md` §1 currently states: "This rollout is deliberately one phase, not the usual 3–5," gated on an author-stated scope override that no longer applies per this change's instructions.
- `test-plan.md` §2 already lists Risk #2 in the Risk Map table (`ProjectWorkspaceViewModel` regressing silently) but has **no Risk Response Guidance row** for it — only Risk #1 has one.
- `test-plan.md` §3 Phased Rollout has exactly one row (Phase 1, status `complete`, linked to `context/archive/2026-09-14-testing-critical-path-persistence/`). No Phase 2 row exists yet.
- `test-plan.md` §7 "What We Deliberately Don't Test" lists Risks #2–#4 as "explicitly deferred... per the author's Q1/Q5 scope override" — this line becomes stale once Risk #2 is promoted to an active phase.
- Research (`context/changes/test-plan-refresh-2026-09-14/research.md`) confirmed, with file:line references, that:
  - `ProjectWorkspaceViewModel.UpdateTask(RenovationTask)` (`src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs:101-177`) and `DeleteTask(Guid)` (`ProjectWorkspaceViewModel.cs:187-214`) are the real mutation entry points behind the UI's Edit/Delete commands.
  - `TaskListViewModel` and `PlanCanvasViewModel` do **not** observe `Project.Tasks` live (no `CollectionChanged` subscriptions anywhere) — they are refreshed only via explicit `TaskList.Refresh(_project.Tasks)` / `Canvas.UpdateMarker(task)` / `Canvas.RemoveMarker(taskId)` calls made by the parent, and only after `TrySave()` (`ProjectWorkspaceViewModel.cs:221-233`) returns `true`.
  - This confirms the risk's core assumption to challenge: in-memory mutation does **not** imply the panels refresh — the parent must explicitly push the update.
- The existing Phase 1 test (`tests/BeaverWorks.Desktop.Tests/ProjectWorkspacePersistenceTests.cs:1-72`) already establishes the exact fixture/construction/reload conventions this phase reuses: temp-directory + real PNG fixture copy, direct `ProjectWorkspaceViewModel` construction with `new ProjectStore()` + `new UserBudgetProfileStore(tempDir)`, and a reload via a **second**, independent `ProjectStore().Load(...)` + a **second** `ProjectWorkspaceViewModel` instance.
- `TaskMarkerViewModel` (`src/BeaverWorks.Desktop/ViewModels/TaskMarkerViewModel.cs:15-37`) exposes the underlying `Task` (a `RenovationTask`) as a public property, so `Canvas.Markers` entries can be asserted against directly (`marker.Task.Title`, `marker.Task.Id`).
- `Project.GetDependents(Guid)` (`src/BeaverWorks.Core/Models/Project.cs`) returns tasks that depend on a given task; `DeleteTask` silently no-ops if `dependents.Count > 0` — the deleted task in this phase's test must have zero dependents to actually delete.

## Desired End State

1. `test-plan.md` reads as the rollout's current source of truth: the §1 override note is retired, §2 has a Risk Response Guidance row for Risk #2, §3 has a Phase 2 row (status `complete`, linked to this change folder), §7 no longer lists Risk #2 as deferred, and §8's Freshness Ledger date is bumped.
2. `tests/BeaverWorks.Desktop.Tests/` has one new `[Theory]` integration test proving that both an edit and a delete via `ProjectWorkspaceViewModel` are (a) saved to disk, (b) reflected in `TaskList.Tasks`, and (c) reflected in `Canvas.Markers` — checked both immediately after the mutation and after a genuine reload (fresh `ProjectStore.Load` + second `ProjectWorkspaceViewModel`).

**Verification**: `dotnet test tests\BeaverWorks.Desktop.Tests\BeaverWorks.Desktop.Tests.csproj` passes (now 3 test cases: the existing Phase 1 fact + the new theory's 2 cases), and `dotnet build BeaverWorks.sln --no-restore` still produces 0 warnings.

## What We're NOT Doing

- Not adding a save-failure/rollback test — happy-path only, matching Phase 1's precedent and this rollout's "minimal scope, one additional integration test" instruction. `TrySave()`'s rollback branches in `UpdateTask`/`DeleteTask` remain untested by this phase.
- Not testing `UpdateTaskStatus`/the quick-status-change path, `AddTask`'s panel-refresh behavior (already implicitly covered by Phase 1), or budget/recommendation recomputation triggered by status changes — out of Risk #2's stated scope (mutation/save/refresh wiring, not budget logic).
- Not adding FlaUI/UI automation — `test-plan.md` §4/§5 already scopes this rollout to the integration layer only; no CI/CD exists or is being added (`AGENTS.md` hard rule).
- Not retargeting or duplicating the test project — reuses the existing `BeaverWorks.Desktop.Tests` project scaffolded by Phase 1.
- Not asserting `Rows`/`DoneRows` (the recommendation-derived collections on `TaskListViewModel`) — only `Tasks` (the collection `Refresh(...)` populates directly) is in scope; `Rows`/`DoneRows` depend on `UpdateRecommendations`, a separate mechanism from the one this risk targets.

## Implementation Approach

Phase 1 updates `test-plan.md` alone (no code), so the doc accurately reflects the rollout before the code that satisfies it exists. Phase 2 adds the test, reusing the Phase 1 test project and its fixture asset (`Assets/sample-floor-plan.png` already copied there) — no new project, no new fixture. The new test class follows the same `IDisposable` + temp-directory convention as `ProjectWorkspacePersistenceTests`, but is parameterized (`[Theory]`) over edit and delete so one test file proves both mutation paths without duplicating fixture/arrange code.

## Phase 1: Refresh test-plan.md

### Overview

Update the frozen strategy document so it accurately reflects that Risk #2 is now an active rollout phase, not a deferred item.

### Changes Required:

#### 1. Retire the §1 scope override

**File**: `context/foundation/test-plan.md`

**Intent**: Remove the now-stale claim that the rollout is deliberately limited to one phase, since Risk #2 is being promoted to Phase 2.

**Contract**: In the "Author-stated scope override (interview Q1)" paragraph, replace the sentence "This rollout is deliberately one phase, not the usual 3–5." with a note that the override has been superseded and the rollout now covers Phase 2 per Risk #2. Leave the rest of the paragraph (the original Q1 interview record) intact as historical context — do not delete the paragraph, only correct the now-false closing claim.

#### 2. Add Risk #2 Risk Response Guidance row

**File**: `context/foundation/test-plan.md`

**Intent**: Give Risk #2 the same "what would prove protection / must challenge / research grounding / cheapest layer / anti-pattern" treatment §2 already gives Risk #1, using the guidance supplied in this change's Notes and confirmed by research.

**Contract**: Add a second row to the existing Risk Response Guidance table (same 5 columns: Risk, What would prove protection, Must challenge, Context `/10x-research` must ground, Likely cheapest layer, Anti-pattern to avoid) for Risk #2, populated from `context/changes/test-plan-refresh-2026-09-14/research.md`'s Summary and this change's Notes — in particular: proof = mutation via `ProjectWorkspaceViewModel` is persisted via save AND reflected in `TaskList`/`Canvas`; must-challenge = in-memory mutation implies panel refresh (confirmed false); grounding = `UpdateTask`/`DeleteTask` wiring, `TaskList.Refresh`/`Canvas.UpdateMarker`/`RemoveMarker`, save-then-refresh ordering (all now documented in this change's research.md); cheapest layer = integration, no FlaUI; anti-pattern = asserting only that `Project.Tasks` changed without checking `TaskList.Tasks`/`Canvas.Markers`.

#### 3. Add Phase 2 rollout row

**File**: `context/foundation/test-plan.md`

**Intent**: Record Risk #2 as an actively-rolled-out phase, matching the Phase 1 row's shape.

**Contract**: Add a row to the §3 Phased Rollout table: `| 2 | ProjectWorkspaceViewModel save/refresh coverage | Prove a task edit and delete via ProjectWorkspaceViewModel is persisted via save and reflected in both TaskList and Canvas panel ViewModels | #2 | integration | complete | context/changes/test-plan-refresh-2026-09-14/ |`. Update the paragraph below the table (currently "This rollout intentionally has one phase...") to reflect that the rollout now has two phases and Risks #3–#4 remain deferred.

#### 4. Remove Risk #2 from §7's deferred list

**File**: `context/foundation/test-plan.md`

**Intent**: Keep §7 ("What We Deliberately Don't Test") accurate — Risk #2 is no longer deferred.

**Contract**: In the bullet listing "Risks #2–#4 in §2 (ViewModel wiring glue code, `App.xaml.cs` composition-root churn, auth/login regressions)," narrow it to "Risks #3–#4" and drop the ViewModel-wiring-glue-code clause (now covered), leaving `App.xaml.cs` composition-root churn and auth/login regressions as the still-deferred items.

#### 5. Bump the Freshness Ledger

**File**: `context/foundation/test-plan.md`

**Intent**: Reflect that the strategy section was reviewed today as part of this refresh.

**Contract**: Update the §8 "Strategy (§1–§5) last reviewed:" date to today's date (2026-09-14; already current, confirm it stays in sync — no change needed if already dated today).

### Success Criteria:

#### Automated Verification:

- `test-plan.md` still renders as valid Markdown with no broken table rows (visually verified by re-reading the diff; no automated linter exists for this doc)

#### Manual Verification:

- §1, §2, §3, §7, and §8 all read consistently — no remaining reference implies the rollout is limited to one phase or that Risk #2 is still deferred

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 2: Implement the ProjectWorkspaceViewModel save/refresh integration test

### Overview

Add one `[Theory]` integration test proving both `UpdateTask` and `DeleteTask` persist via save and are reflected in `TaskList.Tasks` and `Canvas.Markers`, both immediately and after a genuine reload.

### Changes Required:

#### 1. New test class

**File**: `tests/BeaverWorks.Desktop.Tests/ProjectWorkspaceMutationRefreshTests.cs` (new)

**Intent**: Prove Risk #2: after an edit or delete via `ProjectWorkspaceViewModel`, the mutation is (a) persisted to disk and (b) reflected in both panel ViewModels — not just in the in-memory `Project.Tasks` — both before and after a genuine reload.

**Contract**: `IDisposable` test class following the same temp-directory + real-PNG-fixture convention as `ProjectWorkspacePersistenceTests` (copy `Assets/sample-floor-plan.png` into a per-test temp dir; `ProjectStore`/`UserBudgetProfileStore` real instances, no mocks).

Test body shape (parameterized `[Theory]` with two cases — edit and delete):
1. Arrange: build a `Project` with the real image fixture and one pre-existing `RenovationTask` (via `RenovationTask.Create(...)`, zero `DependsOnTaskIds` so delete isn't blocked by `Project.GetDependents`). Construct `ProjectWorkspaceViewModel` #1 from it (this seeds `TaskList`/`Canvas` from the pre-existing task through the constructor, not through a mutation call).
2. Act — edit case: call `workspace.UpdateTask(...)` with the same `Id` but a changed `Title`. Act — delete case: call `workspace.DeleteTask(taskId)`.
3. Assert (same-session, before reload):
   - Edit case: `workspace.Project.Tasks` contains the task with the new title; `workspace.TaskList.Tasks` contains a task with the same `Id` and the new title (not the same `RenovationTask` reference as `Project.Tasks`'s entry is acceptable — assert by value, matching the "not just the underlying model" requirement); `workspace.Canvas.Markers` contains a marker whose `Task.Title` is the new title.
   - Delete case: `workspace.Project.Tasks` no longer contains the task; `workspace.TaskList.Tasks` no longer contains it; `workspace.Canvas.Markers` no longer contains a marker for that `Id`.
4. Reload: `var reloadedProject = new ProjectStore().Load(_projectFilePath);` then construct a **second**, independent `ProjectWorkspaceViewModel` from it (new `ProjectStore()`/`UserBudgetProfileStore` instances, never reusing step-1 references — matches Phase 1's convention).
5. Repeat the same three-way assertion (steps 3's edit/delete-specific checks) against the reloaded workspace's `Project.Tasks`, `TaskList.Tasks`, and `Canvas.Markers`.

This deliberately asserts all three surfaces (model, task list, canvas) at each checkpoint — asserting only `Project.Tasks` changed (the anti-pattern named in the Risk Response Guidance) would still pass if `TaskList.Refresh(...)`/`Canvas.UpdateMarker`/`RemoveMarker` were accidentally removed from `UpdateTask`/`DeleteTask`; this test would catch that regression where a model-only assertion would not.

#### 2. Cookbook per-rollout-phase note

**File**: `context/foundation/test-plan.md`

**Intent**: Fill in §6.4 ("Per-rollout-phase notes," currently "(Empty...)") with the pattern this phase establishes, per the test-plan's convention that each rollout phase updates the cookbook.

**Contract**: Add a short entry under §6.4 noting: `ProjectWorkspaceMutationRefreshTests` (Phase 2) established the "assert all three surfaces" pattern — when a test's job is to prove a ViewModel-driven mutation propagates correctly, assert the source-of-truth model AND every panel ViewModel that's supposed to reflect it, not just the model; do this both immediately after the mutation and after a genuine reload.

### Success Criteria:

#### Automated Verification:

- New theory test passes, both cases: `dotnet test tests\BeaverWorks.Desktop.Tests\BeaverWorks.Desktop.Tests.csproj`
- Full solution build still 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- Existing test suites unaffected: `dotnet test tests\BeaverWorks.Core.Tests\BeaverWorks.Core.Tests.csproj`
- Existing Phase 1 test in the same project still passes (no fixture/temp-directory interference between test classes): confirmed by the same `dotnet test tests\BeaverWorks.Desktop.Tests\BeaverWorks.Desktop.Tests.csproj` run reporting 3 total test cases passing (1 Phase 1 fact + 2 Phase 2 theory cases)

#### Manual Verification:

- Re-running the test project twice in a row passes both times, confirming no leftover temp-directory state leaks between runs (same check Phase 1 already performs)
- `test-plan.md` §6.4 reads as a usable, concrete note rather than the placeholder "(Empty...)" text

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Testing Strategy

### Unit Tests:

- None added — this phase is integration-level only; existing unit tests (`PlanCoordinateMapperTests`, `ProjectStoreTests`, `RenovationTaskTests`) are unchanged.

### Integration Tests:

- `ProjectWorkspaceMutationRefreshTests` (new, Phase 2): one `[Theory]` with 2 cases (edit, delete), each asserting `Project.Tasks`/`TaskList.Tasks`/`Canvas.Markers` agreement both before and after a genuine reload.
- `ProjectWorkspacePersistenceTests` (existing, Phase 1): unaffected, must continue passing alongside the new test class in the same project.

### Manual Testing Steps:

1. Run `dotnet test tests\BeaverWorks.Desktop.Tests\BeaverWorks.Desktop.Tests.csproj` locally twice in a row and confirm all 3 test cases pass both times, with no interactive session required.
2. Temporarily comment out `TaskList.Refresh(_project.Tasks)` in `ProjectWorkspaceViewModel.UpdateTask` (or the equivalent line in `DeleteTask`) and confirm the new theory test fails — a sanity check that it actually catches the panel-refresh regression the risk names — then revert.
3. Confirm `test-plan.md` §1, §2, §3, §7, §8 read consistently end-to-end after the Phase 1 edits.

## Performance Considerations

None — two additional in-process test cases, temp-file-backed, run in milliseconds; no performance budget applies.

## Migration Notes

Not applicable — no production code changes, no data model changes, no new test project.

## References

- Related research: `context/changes/test-plan-refresh-2026-09-14/research.md`
- Test-plan rollout definition: `context/foundation/test-plan.md` §2 (Risk #2), §3 (Phase 1 precedent)
- Pattern precedent: `tests/BeaverWorks.Desktop.Tests/ProjectWorkspacePersistenceTests.cs:1-72`
- Historical design intent: `context/archive/2026-09-14-testing-critical-path-persistence/plan.md`, `context/archive/2026-09-14-testing-critical-path-persistence/research.md`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Refresh test-plan.md

#### Automated

- [x] 1.1 test-plan.md still renders as valid Markdown with no broken table rows — fcff9f3

#### Manual

- [x] 1.2 §1, §2, §3, §7, and §8 all read consistently — fcff9f3

### Phase 2: Implement the ProjectWorkspaceViewModel save/refresh integration test

#### Automated

- [x] 2.1 New theory test passes, both cases — 2fef258
- [x] 2.2 Full solution build still 0 warnings — 2fef258
- [x] 2.3 Existing test suites unaffected — 2fef258
- [x] 2.4 Existing Phase 1 test in the same project still passes — 2fef258

#### Manual

- [x] 2.5 Re-running the test project twice in a row passes both times — 2fef258
- [x] 2.6 test-plan.md §6.4 reads as a usable, concrete note — 2fef258
