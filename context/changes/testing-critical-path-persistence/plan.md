# Critical-Path Persistence Coverage Implementation Plan

## Overview

Add a new `BeaverWorks.Desktop.Tests` project and write one integration test
that proves Risk #1 from `context/foundation/test-plan.md`: after
create/open project → pin a task on the plan → save → close/reopen, the
reloaded project exposes the task with an identical title and identical
marker position. The test exercises the real production save trigger
(`ProjectWorkspaceViewModel.AddTask` → `TrySave`) and a genuinely
independent reload (`ProjectStore.Load` into a second
`ProjectWorkspaceViewModel`), which no existing test does today.

## Current State Analysis

- `PlanCoordinateMapperTests` (`tests/BeaverWorks.Core.Tests/Services/PlanCoordinateMapperTests.cs:17-96`) proves click→coordinate math only, with `Assert.Equal(expected, actual, precision: 10)`.
- `ProjectStoreTests.SaveAndLoad_RoundTripsProjectWithTasks_PositionPreservedExactly` (`tests/BeaverWorks.Core.Tests/Persistence/ProjectStoreTests.cs:29-50`) proves the raw JSON round-trip via direct `ProjectStore.Save`/`Load` calls — it never touches `ProjectWorkspaceViewModel`, so it doesn't prove the actual save trigger used by the app (`AddTask` → `TrySave`) does the right thing, nor that a second, independently-constructed ViewModel reconstructs the same object graph.
- `ProjectWorkspaceViewModel` (`src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs:52`) lives in `BeaverWorks.Desktop` (`net10.0-windows`, `UseWPF=true`). The only existing test project, `BeaverWorks.Core.Tests`, targets plain `net10.0` and only references `BeaverWorks.Core` (`tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`) — it cannot reference `BeaverWorks.Desktop` without retargeting, which would blur the deliberate Core/Desktop separation.
- `BeaverWorks.UiTests` already targets `net10.0-windows`/`UseWPF=true` (`tests/BeaverWorks.UiTests/BeaverWorks.UiTests.csproj`) but drives the real running app via FlaUI/UI Automation, which needs an interactive desktop session — explicitly not usable for this risk per `test-plan.md` §4/§5 (confirmed with the author).
- `ProjectStore` (`src/BeaverWorks.Core/Persistence/ProjectStore.cs:14-41`) has no caching: `Load` always deserializes fresh from disk via `System.Text.Json`. `PlanPoint` (`src/BeaverWorks.Core/Models/PlanPoint.cs:3-12`) stores `X`/`Y` at full double precision with no rounding.
- No rounding-margin constant exists anywhere in production code despite the PRD's "established rounding margin" wording (`context/foundation/prd.md:91-93`).

### Key Discoveries:

- `ProjectWorkspaceViewModel(Project project, string projectFilePath, IProjectStore projectStore, string username, IUserBudgetProfileStore budgetProfileStore)` (`ProjectWorkspaceViewModel.cs:52`) can be constructed directly in a test — no `App.xaml.cs`/dialog wiring needed.
- `AddTask(RenovationTask task)` (`ProjectWorkspaceViewModel.cs:75-88`) is the real save trigger: mutates `_project.Tasks`, calls `TrySave()` (`ProjectWorkspaceViewModel.cs:221-231`), which calls `_projectStore.Save(...)`.
- The constructor eagerly builds `Canvas = new PlanCanvasViewModel(project)` (`ProjectWorkspaceViewModel.cs:59`), which decodes `project.PlanImagePath` into a `BitmapImage` via `BeginInit()`/`EndInit()` (`PlanCanvasViewModel.cs:36-42`) — this requires a **real, decodable image file**, not the 4-byte magic-number stub `ProjectStoreTests` uses (`ProjectStoreTests.cs:17`), which is sufficient for `ProjectStore.Load`'s existence-only check but will fail `BitmapImage` decoding.
- The constructor also calls `RecomputeRecommendations()` (`ProjectWorkspaceViewModel.cs:66`), which calls `_budgetProfileStore.Load(_username)` (`ProjectWorkspaceViewModel.cs:296-300`) — a real `UserBudgetProfileStore(tempRoot)` (`src/BeaverWorks.Core/Persistence/UserBudgetProfileStore.cs`) satisfies this by returning a zero-budget default, matching the project's no-mocking convention (`test-plan.md` §4).
- `RenovationTask.Create(title, position)` (used by `ProjectStoreTests.cs:34` and `NewTaskViewModel.cs`) is the model factory to use for building the pinned task.

## Desired End State

A new `tests/BeaverWorks.Desktop.Tests` project exists, is wired into `BeaverWorks.sln`, and contains one integration test that:
1. Creates a `Project` + real-image fixture in a temp directory.
2. Constructs a `ProjectWorkspaceViewModel` and calls `AddTask(...)` to pin a task with a known title and `PlanPoint`.
3. Constructs a **second**, independent `ProjectStore` load and a **second** `ProjectWorkspaceViewModel` from the reloaded `Project`.
4. Asserts the reloaded task's `Title` and `Position.X`/`Position.Y` match the original.

**Verification**: `dotnet test tests/BeaverWorks.Desktop.Tests/BeaverWorks.Desktop.Tests.csproj` passes, and `dotnet build BeaverWorks.sln --no-restore` still produces 0 warnings.

## What We're NOT Doing

- Not simulating the actual mouse click through `PlanCanvasViewModel.HandleClick`/`PlanCoordinateMapper` — that math is already covered by `PlanCoordinateMapperTests`; this test starts from `AddTask` directly (per author decision).
- Not driving through `RecentProjectsViewModel.TryOpen`/`App.xaml.cs` navigation wiring for the "reopen" step — a second `ProjectStore.Load` + a second `ProjectWorkspaceViewModel` is the narrowest thing that proves Risk #1 (per author decision).
- Not using `BeaverWorks.UiTests`/FlaUI — that layer requires an interactive desktop session and isn't wired to any automated gate; `test-plan.md` already scoped this risk to the integration layer (confirmed with the author).
- Not asserting `Status` preservation or `Canvas`/`TaskList` refresh correctness after reload — out of this test's stated scope (title + position only, per author decision). These remain covered at the model level by `RenovationTaskTests` and are not part of Risk #1.
- Not adding a save-failure/rollback test (e.g., read-only file) — happy-path only, per the author's single-meaningful-test scope for Risk #1. Risks #2-#4 (including `TrySave` failure handling more broadly) are explicitly deferred in `test-plan.md` §7.
- Not retargeting `BeaverWorks.Core.Tests` or adding a Desktop reference to it — a new, separate test project keeps Core's cross-platform-clean boundary intact.
- Not wiring this test into any CI/CD pipeline — none exists and none is being added (`AGENTS.md` hard rule); the test is wired into the local `dotnet test` gate only.

## Implementation Approach

Create a small, focused xUnit test project (`BeaverWorks.Desktop.Tests`) modeled on the existing `BeaverWorks.UiTests` project's TFM/WPF settings (needed only because `ProjectWorkspaceViewModel.Canvas.PlanImage` is a `BitmapImage`-typed public property, not because any UI automation happens), but without FlaUI — this test never renders a window or needs an interactive session. It follows the same temp-directory, no-mocking convention as `ProjectStoreTests`/`UserBudgetProfileStoreTests`.

## Critical Implementation Details

### Plan-image decoding is eager and real

`PlanCanvasViewModel`'s constructor calls `BitmapImage.BeginInit()`/`EndInit()` against `project.PlanImagePath` before any control size is set — this happens synchronously during `ProjectWorkspaceViewModel` construction, for both the first and the second (reloaded) instance. The test fixture's plan-image file must be a real, decodable image (e.g. a valid minimal PNG), not a placeholder byte stub — `ProjectStoreTests`' 4-byte magic-number file (`ProjectStoreTests.cs:17`) is not sufficient here and will throw during `BitmapImage` decoding. Copying the existing `src/BeaverWorks.Desktop/Assets/sample-floor-plan.png` as a test-fixture asset (or embedding a small valid PNG byte array) both satisfy this; either is acceptable.

## Phase 1: Scaffold BeaverWorks.Desktop.Tests project

### Overview

Create the new test project, wire it into the solution, and prove the wiring works end-to-end with a trivial placeholder test before writing the real integration test.

### Changes Required:

#### 1. New test project

**File**: `tests/BeaverWorks.Desktop.Tests/BeaverWorks.Desktop.Tests.csproj`

**Intent**: A new xUnit test project that can reference `BeaverWorks.Desktop` (and transitively `BeaverWorks.Core`) to exercise `ProjectWorkspaceViewModel` directly, without any UI automation or interactive-session requirement.

**Contract**: `TargetFramework=net10.0-windows`, `UseWPF=true` (required to resolve `BitmapImage`/`System.Windows` types referenced by `BeaverWorks.Desktop`'s public API surface — not for rendering), `IsPackable=false`, package references matching `BeaverWorks.Core.Tests` (`Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `coverlet.collector`, same versions), `<Using Include="Xunit" />`, and a single `ProjectReference` to `..\..\src\BeaverWorks.Desktop\BeaverWorks.Desktop.csproj`.

#### 2. Solution wiring

**File**: `BeaverWorks.sln`

**Intent**: Register the new project under the existing `tests` solution folder, matching the existing `BeaverWorks.Core.Tests`/`BeaverWorks.UiTests` entries.

**Contract**: Use `dotnet sln BeaverWorks.sln add tests\BeaverWorks.Desktop.Tests\BeaverWorks.Desktop.Tests.csproj --solution-folder tests` rather than hand-editing the `.sln` file, so GUIDs and section formatting stay consistent with the existing entries.

#### 3. Plan-image test fixture asset

**File**: `tests/BeaverWorks.Desktop.Tests/Assets/sample-floor-plan.png` (new)

**Intent**: A real, decodable PNG usable as `Project.PlanImagePath` in tests, per the "Plan-image decoding is eager" constraint above.

**Contract**: Copy `src/BeaverWorks.Desktop/Assets/sample-floor-plan.png` into the new project's `Assets/` folder, added as `<Content Include="Assets\sample-floor-plan.png"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content>` (mirrors `BeaverWorks.Desktop.csproj`'s own content item), so tests can reference it via a path under `AppContext.BaseDirectory`.

#### 4. Placeholder test

**File**: `tests/BeaverWorks.Desktop.Tests/PlaceholderTests.cs` (new, temporary — replaced by the real test class in Phase 2; delete this file in Phase 2 rather than leaving it alongside)

**Intent**: One trivial `[Fact]` that constructs a `ProjectWorkspaceViewModel` against the copied asset and asserts it doesn't throw, proving the project reference, TFM, and asset wiring all work before Phase 2's real assertions.

**Contract**: No specific signature beyond a passing `[Fact]`.

### Success Criteria:

#### Automated Verification:

- Solution restores cleanly: `dotnet restore BeaverWorks.sln`
- Solution builds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- New project's placeholder test passes: `dotnet test tests\BeaverWorks.Desktop.Tests\BeaverWorks.Desktop.Tests.csproj`

#### Manual Verification:

- `BeaverWorks.Desktop.Tests` appears under the `tests` solution folder when the solution is opened (e.g. in an IDE), alongside `BeaverWorks.Core.Tests` and `BeaverWorks.UiTests`.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 2: Implement the critical-path persistence integration test

### Overview

Replace the placeholder test with the real integration test proving Risk #1, and update the test-plan cookbook with the pattern this phase establishes.

### Changes Required:

#### 1. Integration test class

**File**: `tests/BeaverWorks.Desktop.Tests/ProjectWorkspacePersistenceTests.cs` (new; delete `PlaceholderTests.cs` from Phase 1)

**Intent**: One `[Fact]` proving that a task pinned via `ProjectWorkspaceViewModel.AddTask` survives a genuine reload (new `ProjectStore.Load` + new `ProjectWorkspaceViewModel` instance) with identical title and marker position.

**Contract**: Follows the `ProjectStoreTests`/`UserBudgetProfileStoreTests` fixture convention: an `IDisposable` test class with a temp directory created in the constructor and deleted in `Dispose()`, containing the copied plan-image asset (from Phase 1) and a `project.bwproj` path.

Test body shape (names illustrative, not prescriptive):
1. Arrange: create a `Project` (`Name`, `PlanImagePath` = the temp-copied real image, empty `Tasks`, `CreatedAt`), a `ProjectStore`, and a `UserBudgetProfileStore(tempRoot)`.
2. Construct `ProjectWorkspaceViewModel` #1 with that `Project` + a fixed `projectFilePath` + the store + a test username + the budget store.
3. Call `AddTask(RenovationTask.Create("<known title>", new PlanPoint { X = <known X>, Y = <known Y> }))` on ViewModel #1 — this both mutates and persists via the real `TrySave()` path.
4. Reload: call `projectStore.Load(projectFilePath)` again (a **second, independent** call — do not reuse the `Project` reference from step 1) to get a fresh `Project` graph, then construct `ProjectWorkspaceViewModel` #2 from it (new `IProjectStore`/`IUserBudgetProfileStore` instances are acceptable but not required, since neither store caches state — the loaded `Project` object graph and the `ProjectWorkspaceViewModel` instance must be new).
5. Assert: exactly one task in the reloaded project; `Title` matches; `Position.X`/`Position.Y` match the original via `Assert.Equal(expected, actual, precision: 10)`.

#### 2. Cookbook update

**File**: `context/foundation/test-plan.md`

**Intent**: Fill in §6.2 ("Adding an integration test") with the pattern this phase established, per the test-plan's stated convention that each rollout phase's plan updates the relevant cookbook entry.

**Contract**: Replace the current `TBD — see §3 Phase 1` line under `### 6.2 Adding an integration test` with: location (`tests/BeaverWorks.Desktop.Tests/`), naming (`<Scenario>Tests.cs`), the reference test (`ProjectWorkspacePersistenceTests.cs`), the run command (`dotnet test tests\BeaverWorks.Desktop.Tests\BeaverWorks.Desktop.Tests.csproj`), and the "why a separate project" note (Desktop ViewModel types require `net10.0-windows`/`UseWPF`, which `BeaverWorks.Core.Tests` deliberately doesn't carry) plus the plan-image-must-be-real-and-decodable gotcha.

### Success Criteria:

#### Automated Verification:

- New integration test passes: `dotnet test tests\BeaverWorks.Desktop.Tests\BeaverWorks.Desktop.Tests.csproj`
- Full solution build still 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- Existing test suites unaffected: `dotnet test tests\BeaverWorks.Core.Tests\BeaverWorks.Core.Tests.csproj`

#### Manual Verification:

- Re-running the test twice in a row (or with `dotnet test` run consecutively) passes both times, confirming no leftover temp-directory state leaks between runs.
- `context/foundation/test-plan.md` §6.2 reads as a usable, concrete recipe (not a placeholder) for the next integration test.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Testing Strategy

### Unit Tests:

- None added — this phase is exclusively integration-level; existing unit tests (`PlanCoordinateMapperTests`, `ProjectStoreTests`, `RenovationTaskTests`) are unchanged.

### Integration Tests:

- `ProjectWorkspacePersistenceTests`: the single scenario described in Phase 2 — pin → save → reload via a new instance → title + position preserved.

### Manual Testing Steps:

1. Run `dotnet test tests\BeaverWorks.Desktop.Tests\BeaverWorks.Desktop.Tests.csproj` locally and confirm it passes without an interactive session (no window is rendered).
2. Temporarily corrupt the reload step (e.g. reuse the original `Project` reference instead of reloading) and confirm the test would NOT catch that regression as a sanity check that it's asserting against the reloaded object, not the original — then revert.
3. Delete the temp directory manually mid-run (not expected, just confirms `Dispose()` cleanup logic doesn't throw if the directory is already gone).

## Performance Considerations

None — single test, temp-file-backed, runs in milliseconds; no performance budget applies.

## Migration Notes

Not applicable — new test project and new test only, no production code or data model changes.

## References

- Related research: `context/changes/testing-critical-path-persistence/research.md`
- Test-plan rollout definition: `context/foundation/test-plan.md` §2 (Risk #1), §3 (Phase 1)
- Pattern precedent: `tests/BeaverWorks.Core.Tests/Persistence/ProjectStoreTests.cs:12-50`
- Historical design intent: `context/archive/2026-09-13-pin-and-persist-task/plan.md:378-401,496-502`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Scaffold BeaverWorks.Desktop.Tests project

#### Automated

- [x] 1.1 Solution restores cleanly — 521ea61
- [x] 1.2 Solution builds with 0 warnings — 521ea61
- [x] 1.3 New project's placeholder test passes — 521ea61

#### Manual

- [x] 1.4 BeaverWorks.Desktop.Tests appears under the tests solution folder — 521ea61

### Phase 2: Implement the critical-path persistence integration test

#### Automated

- [x] 2.1 New integration test passes — 6bf5f44
- [x] 2.2 Full solution build still 0 warnings — 6bf5f44
- [x] 2.3 Existing test suites unaffected — 6bf5f44

#### Manual

- [x] 2.4 Re-running the test twice in a row passes both times — 6bf5f44
- [x] 2.5 test-plan.md §6.2 reads as a usable, concrete recipe — 6bf5f44
